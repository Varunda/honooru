﻿using FFMpegCore.Enums;
using honooru.Code;
using honooru.Code.ExtensionMethods;
using honooru.Code.Hubs;
using honooru.Code.Hubs.Implementations;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.App.MediaUploadStep;
using honooru.Models.Config;
using honooru.Models.Db;
using honooru.Models.Internal;
using honooru.Models.Queues;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Queues;
using honooru.Services.Repositories;
using honooru.Services.UploadStepHandler;
using honooru.Services.UrlMediaExtrator;
using honooru.Services.Util;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [Route("/api/media-asset")]
    [ApiController]
    public class MediaAssetApiController : ApiControllerBase {

        private readonly ILogger<MediaAssetApiController> _Logger;

        private readonly IOptions<StorageOptions> _StorageOptions;

        private readonly IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> _MediaAssetHub;

        private readonly AppCurrentAccount _CurrentAccount;
        private readonly PostDb _PostDb;
        private readonly MediaAssetRepository _MediaAssetRepository;
        private readonly FileExtensionService _FileExtensionHelper;
        private readonly UploadStepsProcessor _UploadStepsHandler;
        private readonly UrlMediaExtractorHandler _UrlExtractor;

        private readonly BaseQueue<UploadSteps> _UploadStepsQueue;

        public MediaAssetApiController(ILogger<MediaAssetApiController> logger,
            IOptions<StorageOptions> storageOptions,
            PostDb postDb, AppCurrentAccount currentAccount,
            MediaAssetRepository mediaAssetRepository, FileExtensionService fileExtensionHelper,
            IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> mediaAssetHub,
            UploadStepsProcessor uploadStepsHandler, BaseQueue<UploadSteps> uploadStepsQueue,
            UrlMediaExtractorHandler urlExtractor) {

            _Logger = logger;

            _StorageOptions = storageOptions;

            _MediaAssetHub = mediaAssetHub;

            _PostDb = postDb;
            _CurrentAccount = currentAccount;
            _MediaAssetRepository = mediaAssetRepository;
            _FileExtensionHelper = fileExtensionHelper;
            _UploadStepsHandler = uploadStepsHandler;
            _UploadStepsQueue = uploadStepsQueue;
            _UrlExtractor = urlExtractor;
        }

        /// <summary>
        ///     get a <see cref="MediaAsset"/> by its <see cref="MediaAsset.Guid"/>
        /// </summary>
        /// <param name="guid">ID of the <see cref="MediaAsset"/> to get</param>
        /// <response code="200">
        ///     the response will contain the <see cref="MediaAsset"/> with
        ///     <see cref="MediaAsset.Guid"/> of <paramref name="guid"/>
        /// </response>
        /// <response code="204">
        ///     no <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="guid"/> exists
        /// </response>
        [HttpGet("{guid}")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<MediaAsset>> GetByGuid(Guid guid) {
            MediaAsset? asset = await _MediaAssetRepository.GetByID(guid);

            if (asset == null) {
                return ApiNoContent<MediaAsset>();
            }

            Thing[] arr = [];
            arr[0].A = "abc";

            return ApiOk(asset);
        }

        struct Thing {
            public string A { get; set; } = "";

            public Thing(string a) {
                A = a;
            }

        }

        /// <summary>
        ///     get a list of <see cref="MediaAsset"/>s that are currently, or are queued for, processing
        /// </summary>
        /// <response code="200">
        ///     the response will contain a list of <see cref="MediaAsset"/>
        ///     with a <see cref="MediaAsset.Status"/> of <see cref="MediaAssetStatus.PROCESSING"/>
        ///     or <see cref="MediaAssetStatus.QUEUED"/>
        /// </response>
        [HttpGet("processing")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<List<MediaAsset>>> GetProcessing() {
            List<MediaAsset> assets = await _MediaAssetRepository.GetByStatus(MediaAssetStatus.PROCESSING);
            List<MediaAsset> queued = await _MediaAssetRepository.GetByStatus(MediaAssetStatus.QUEUED);

            assets.AddRange(queued);
            return ApiOk(assets);
        }

        /// <summary>
        ///     get a list of <see cref="MediaAsset"/>s that are ready to be tagged
        /// </summary>
        /// <response code="200">
        ///     the response will contain a list of <see cref="MediaAsset"/>
        ///     with a <see cref="MediaAsset.Status"/> of <see cref="MediaAssetStatus.DONE"/>
        /// </response>
        [HttpGet("ready")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<List<MediaAsset>>> GetReady() {
            List<MediaAsset> assets = await _MediaAssetRepository.GetByStatus(MediaAssetStatus.DONE);
            return ApiOk(assets);
        }

        /// <summary>
        ///     upload a file via a URL
        /// </summary>
        /// <param name="url">URL to upload</param>
        /// <response code="200">
        ///     the response will contain the <see cref="MediaAsset"/> that was uploaded.
        ///     it may not be ready for viewing, check <see cref="MediaAsset.Status"/> for that
        /// </response>
        /// <response code="400">
        ///     one of the following validation errors occured:
        ///     <ul>
        ///         <li>the URL supplied could not be used to pull the file from</li>
        ///     </ul>
        /// </response>
        [HttpPost("upload-url")]
        public async Task<ApiResponse<MediaAsset>> UploadUrl([FromQuery] string url) {
            _Logger.LogInformation($"uploading from url [url={url}]");

            if (_UrlExtractor.CanHandle(url) == false) {
                return ApiBadRequest<MediaAsset>($"no extractor is setup to handle this type of url: '{url}'");
            }

            MediaAsset asset = new();
            asset.Guid = Guid.NewGuid();
            asset.Source = url;
            asset.Timestamp = DateTime.UtcNow;

            // since no md5 currently exists due to the file not being extracted, get one based on the GUID
            byte[] md5 = MD5.HashData(asset.Guid.ToByteArray());
            string md5Str = string.Join("", md5.Select(iter => iter.ToString("x2"))); // turn that md5 into a string
            asset.MD5 = md5Str;

            await _MediaAssetRepository.Upsert(asset);

            if (_UrlExtractor.NeedsQueue(url)) {
                asset.Status = MediaAssetStatus.EXTRACTING;
                await _MediaAssetRepository.Upsert(asset);

                UploadSteps steps = new(asset, _StorageOptions.Value);
                steps.AddExtractStep(url);
                steps.AddReencodeStep(VideoCodec.LibX264, AudioCodec.Aac);
                steps.AddFinalMoveStep();

                _UploadStepsQueue.Queue(steps);

                return ApiOk(asset);
            } else {
                // do nothing with the progress callback here, not needed
                asset = await _UrlExtractor.HandleUrl(asset, url, (progress) => { });

                string filePath = Path.Combine(_StorageOptions.Value.RootDirectory, "upload", asset.Guid + "." + asset.FileExtension);
                using FileStream targetFile = System.IO.File.OpenRead(filePath);

                ApiResponse<MediaAsset> r = await _HandleAsset(asset, targetFile, filePath);

                return r;
            }
        }

        /// <summary>
        ///     upload a new media asset
        /// </summary>
        /// <remarks>
        ///     example cURL request to upload:
        ///     <code>curl.exe -X POST -H "Content-Type: multipart/form-data" -F "data=@test.mkv" "https://localhost:6001/api/post/upload"</code>
        /// </remarks>
        /// <response code="200">
        ///     the response will contain a <see cref="MediaAsset"/> created for this file being uploaded
        /// </response>
        /// <response code="302">
        ///     a <see cref="Post"/> with a <see cref="Post.MD5"/> of the uploaded file already exists.
        ///     the "Location" header will have the url to get the post
        /// </response>
        /// <response code="400">
        ///     one of the following errors took place:
        ///     <ul>
        ///         <li>the ContentType header did not start with "multipart/" </li>
        ///         <li>the boundary of the content type was missing</li>
        ///         <li>no content disposition exists</li>
        ///         <li>the extension from the uploaded file is missing</li>
        ///         <li>the extension from the uploaded file is invalid</li>
        ///     </ul>
        /// </response>
        [HttpPost("upload")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        [RequestTimeout(1000 * 60 * 10)] // 1000ms, 60 seconds, 10 minutes
        public async Task<ApiResponse<MediaAsset>> Upload() {
            string contentType = Request.ContentType ?? "";
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase)) {
                return ApiBadRequest<MediaAsset>($"ContentType '{contentType}' is not a multipart-upload");
            }

            MediaTypeHeaderValue type = MediaTypeHeaderValue.Parse(Request.ContentType);
            string boundary = HeaderUtilities.RemoveQuotes(type.Boundary).Value ?? "";

            if (string.IsNullOrWhiteSpace(boundary)) {
                return ApiBadRequest<MediaAsset>($"boundary from ContentType '{Request.ContentType}' was is null or empty ({type}) ({type.Boundary})");
            }

            MultipartReader reader = new(boundary, Request.Body);
            MultipartSection? part = await reader.ReadNextSectionAsync();

            Guid assetID = Guid.NewGuid();

            MediaAsset asset = new();
            asset.Guid = assetID;

            if (part == null) {
                return ApiBadRequest<MediaAsset>($"Multipart section missing");
            }

            bool hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(part.ContentDisposition, out ContentDispositionHeaderValue? contentDisposition);
            if (hasContentDispositionHeader == false) {
                return ApiInternalError<MediaAsset>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
            }

            if (contentDisposition == null) {
                return ApiInternalError<MediaAsset>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
            }

            if (!HasFileContentDisposition(contentDisposition)) {
                _Logger.LogError($"not a file content disposition");
                return ApiBadRequest<MediaAsset>($"not a file content disposition");
            }

            string originalName = contentDisposition.FileName.Value ?? "";
            asset.FileName = WebUtility.HtmlEncode(originalName) ?? "";

            string? extension = HeaderUtilities.RemoveQuotes(Path.GetExtension(originalName)).Value;
            if (string.IsNullOrWhiteSpace(extension)) {
                return ApiBadRequest<MediaAsset>($"extension from name {originalName} is null or empty");
            }

            extension = extension[1..]; // remove the leading .

            if (_FileExtensionHelper.IsValid(extension) == false) {
                _Logger.LogWarning($"disallowing invalid extension [extension={extension}] [originalName={originalName}]");
                return ApiBadRequest<MediaAsset>($"invalid extension '{extension}' (invalid)");
            }

            string? fileType = _FileExtensionHelper.GetFileType(extension);
            if (fileType == null) {
                _Logger.LogWarning($"failed to get file type from extension [extension={extension}]");
                return ApiBadRequest<MediaAsset>($"invalid extension '{extension}' (failed to get file type)");
            }

            _Logger.LogDebug($"extension found [originalName={originalName}] [extension={extension}] [fileType={fileType}]");
            asset.FileExtension = extension;

            string filePath = Path.Combine(_StorageOptions.Value.RootDirectory, "upload", assetID + "." + extension);
            _Logger.LogInformation($"writing new upload [filePath={filePath}]");
            using FileStream targetFile = System.IO.File.Create(filePath, 1024 * 1024 * 128); // 128 MB buffer instead of the default 4'096
            await part.Body.CopyToAsync(targetFile);
            asset.FileSizeBytes = targetFile.Position;

            ApiResponse<MediaAsset> response = await _HandleAsset(asset, targetFile, filePath);
            
            part = await reader.ReadNextSectionAsync();
            if (part != null) {
                _Logger.LogInformation($"expected part to be null here?");
            }

            return response;
        }

        private async Task<ApiResponse<MediaAsset>> _HandleAsset(MediaAsset asset, Stream targetFile, string filePath) {
            // IMPORTANT: move back to the start of the file so the md5 hashing uses the whole file stream
            targetFile.Seek(0, SeekOrigin.Begin);
            byte[] md5 = await MD5.Create().ComputeHashAsync(targetFile);
            targetFile.Close(); // IMPORTANT: if this is not closed, the handle is left open, and the file cannot be moved
            string md5Str = string.Join("", md5.Select(iter => iter.ToString("x2"))); // turn that md5 into a string
            _Logger.LogInformation($"computed hash [md5={md5Str}] [filePath={filePath}]");

            MD5 hash = MD5.Create();
            hash.TransformBlock([], 0, 0, [], 0);

            // if a post with the md5 of this upload already exists, don't proceed
            Post? existingPost = await _PostDb.GetByMD5(md5Str);
            if (existingPost != null) {
                _Logger.LogDebug($"media asset is already a post [md5Str={md5Str}] [post={existingPost.ID}]");
                try {
                    System.IO.File.Delete(filePath);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to delete existing media asset (post already exists) [filePath={filePath}]");
                }

                return ApiBadRequest<MediaAsset>($"post {existingPost.ID} matched the md5 hash");
            }

            // if a media asset with the md5 of the upload already exists, don't make a new one
            MediaAsset? existingAsset = await _MediaAssetRepository.GetByMD5(md5Str);
            if (existingAsset != null) {
                _Logger.LogDebug($"media asset already pending, returning that one instead [md5Str={md5Str}] [existingAsset={existingAsset.Guid}]");
                try {
                    System.IO.File.Delete(filePath);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to delete existing media asset (post already exists) [filePath={filePath}]");
                }
                return ApiOk(existingAsset);
            }

            asset.MD5 = md5Str;
            asset.Timestamp = DateTime.UtcNow;
            asset.Status = MediaAssetStatus.PROCESSING;

            // files are uploaded with a random GUID, then moved to with a name based on the MD5 hash instead
            string moveInputPath = Path.Combine(_StorageOptions.Value.RootDirectory, "upload", asset.Guid + "." + asset.FileExtension);
            string moveOutputPath = Path.Combine(_StorageOptions.Value.RootDirectory, "work", asset.MD5 + "." + asset.FileExtension);
            _Logger.LogInformation($"moving uploaded file to work directory [input={moveInputPath}] [output={moveOutputPath}");
            System.IO.File.Move(moveInputPath, moveOutputPath);

            await _MediaAssetRepository.Upsert(asset);

            UploadSteps steps = new(asset, _StorageOptions.Value);

            if (asset.FileExtension == "mkv") {
                _Logger.LogTrace($"asset is an mkv, adding a reencode step [asset.Guid={asset.Guid}]");
                steps.AddReencodeStep(VideoCodec.LibX264, AudioCodec.Aac);
            }

            steps.AddFinalMoveStep();

            if (steps.Steps.Count > 1) {
                asset.Status = MediaAssetStatus.QUEUED;
                await _MediaAssetRepository.Upsert(asset);

                _Logger.LogInformation($"queueing for processing [MediaAssetID={steps.Asset.Guid}]");
                _UploadStepsQueue.Queue(steps);
            } else {
                _Logger.LogInformation($"running final move step right away");
                await _UploadStepsHandler.Run(steps, CancellationToken.None);
            }

            _Logger.LogDebug($"all done! [steps.Count={steps.Steps.Count}]");

            return ApiOk(asset);
        }

        public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                    || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }


    }
}
