using FFMpegCore.Enums;
using honooru.Code;
using honooru.Code.ExtensionMethods;
using honooru.Code.Hubs;
using honooru.Code.Hubs.Implementations;
using honooru.Controllers.Api.Upload;
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
    public class MediaAssetApiController : FileUploadBaseController {

        private readonly IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> _MediaAssetHub;

        private readonly UploadStepsProcessor _UploadStepsHandler;
        private readonly UrlMediaExtractorHandler _UrlExtractor;
        private readonly IqdbClient _IqdbClient;

        private readonly BaseQueue<UploadSteps> _UploadStepsQueue;

        public MediaAssetApiController(ILoggerFactory loggerFactory,
            IOptions<StorageOptions> storageOptions,
            PostDb postDb, AppCurrentAccount currentAccount,
            MediaAssetRepository mediaAssetRepository, FileExtensionService fileExtensionHelper,
            IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> mediaAssetHub,
            UploadStepsProcessor uploadStepsHandler, BaseQueue<UploadSteps> uploadStepsQueue,
            UrlMediaExtractorHandler urlExtractor, IqdbClient iqdbClient)
        : base(loggerFactory, currentAccount, mediaAssetRepository, fileExtensionHelper, storageOptions, postDb) {

            _MediaAssetHub = mediaAssetHub;

            _UploadStepsHandler = uploadStepsHandler;
            _UploadStepsQueue = uploadStepsQueue;
            _UrlExtractor = urlExtractor;
            _IqdbClient = iqdbClient;
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

            return ApiOk(asset);
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
            List<MediaAsset> extracting = await _MediaAssetRepository.GetByStatus(MediaAssetStatus.EXTRACTING);

            assets.AddRange(queued);
            assets.AddRange(extracting);
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
            asset.MD5 = md5.JoinToString();

            await _MediaAssetRepository.Upsert(asset);

            if (_UrlExtractor.NeedsQueue(url)) {
                asset.Status = MediaAssetStatus.EXTRACTING;
                await _MediaAssetRepository.Upsert(asset);

                UploadSteps steps = new(asset, _StorageOptions.Value);
                steps.AddExtractStep(url);
                steps.AddReencodeStep(VideoCodec.LibX264, AudioCodec.Aac);
                steps.AddFinalMoveStep();
                steps.AddImageHashStep();

                _UploadStepsQueue.Queue(steps);

                return ApiOk(asset);
            } else {
                // do nothing with the progress callback here, not needed
                asset = await _UrlExtractor.HandleUrl(asset, url, (progress) => { });

                ApiResponse<MediaAsset> r = await _HandleAsset(asset);

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
            MediaAsset asset = new();
            asset.Guid = Guid.NewGuid();
            asset.Timestamp = DateTime.UtcNow;

            // since no md5 currently exists due to the file not being extracted, get one based on the GUID
            byte[] md5 = MD5.HashData(asset.Guid.ToByteArray());
            asset.MD5 = md5.JoinToString();

            await _MediaAssetRepository.Upsert(asset);

            ApiResponse<MediaAsset> response = await ReadSection(asset.Guid, MediaAssetStatus.DEFAULT);

            if (response.Status != 200) {
                return response;
            }

            if (response.Data == null) {
                throw new Exception($"missing data from section read?");
            }

            asset = (MediaAsset)response.Data;
            response = await _HandleAssetUpload(asset);

            return response;
        }

        /// <summary>
        ///     start a multi-chunk upload, returning the <see cref="MediaAsset.Guid"/>
        ///     that will be used for the upload
        /// </summary>
        /// <response code="200">
        ///     the response will contain a <see cref="Guid"/> that is the <see cref="MediaAsset.Guid"/>
        ///     of the <see cref="MediaAsset"/> that was created for the chunked upload
        /// </response>
        [HttpPost("upload/new")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse<Guid>> StartMultipartUpload() {
            Guid uploadId = Guid.NewGuid();

            MediaAsset asset = new();
            asset.Guid = uploadId;
            asset.Timestamp = DateTime.UtcNow;

            // since no md5 currently exists due to the file not being extracted, get one based on the GUID
            byte[] md5 = MD5.HashData(asset.Guid.ToByteArray());
            asset.MD5 = md5.JoinToString();
            asset.Status = MediaAssetStatus.DEFAULT;

            await _MediaAssetRepository.Upsert(asset);

            _Logger.LogInformation($"created new chunked upload [uploadId={asset.Guid}]");

            return ApiOk(uploadId);
        }

        /// <summary>
        ///     complete a multi-part upload
        /// </summary>
        /// <param name="uploadId">ID of the <see cref="MediaAsset"/> to complete</param>
        /// <response code="200">
        ///     the response will contain the <see cref="MediaAsset"/> that was
        ///     just completed upload
        /// </response>
        /// <response code="404">
        ///     no <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="uploadId"/> exists
        /// </response>
        [HttpPost("upload/{uploadId}/done")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse<MediaAsset>> FinishMultipartUpload(Guid uploadId) {
            MediaAsset? asset = await _MediaAssetRepository.GetByID(uploadId);
            if (asset == null) {
                return ApiNotFound<MediaAsset>($"{nameof(MediaAsset)} {uploadId}");
            }

            if (asset.Status != MediaAssetStatus.DEFAULT) {
                return ApiBadRequest<MediaAsset>($"expected {nameof(MediaAsset)} {uploadId} to be {MediaAssetStatus.DEFAULT}, but it was {asset.Status} instead");
            }

            return await _HandleAssetUpload(asset);
        }

        /// <summary>
        ///     upload a chunk of a large file. the body must contain a content type of multipart upload
        /// </summary>
        /// <param name="uploadId">ID of the <see cref="MediaAsset"/> to append this upload to</param>
        /// <response code="200">
        ///     the response will contain the <see cref="MediaAsset"/> whos content was just appended to
        /// </response>
        /// <response code="400">
        ///     the <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="uploadId"/>
        ///     has a <see cref="MediaAsset.Status"/> that is not <see cref="MediaAssetStatus.DEFAULT"/>
        /// </response>
        /// <response code="404">
        ///     no <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="uploadId"/> exists
        /// </response>
        [HttpPost("upload/part")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        [DisableFormValueModelBinding]
        public async Task<ApiResponse<MediaAsset>> UploadPart([FromQuery] Guid uploadId) {
            return await ReadSection(uploadId, MediaAssetStatus.DEFAULT);
        }

        /// <summary>
        ///     common code to handle asset stuff
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        private async Task<ApiResponse<MediaAsset>> _HandleAssetUpload(MediaAsset asset) {
            ApiResponse<MediaAsset> response = await _HandleAsset(asset);
            if (response.Status != 200) {
                return response;
            }

            UploadSteps steps = new(asset, _StorageOptions.Value);

            if (asset.FileExtension == "mkv") {
                _Logger.LogTrace($"asset is an mkv, adding a reencode step [asset.Guid={asset.Guid}]");
                steps.AddReencodeStep(VideoCodec.LibX264, AudioCodec.Aac);
            }

            steps.AddFinalMoveStep();
            steps.AddImageHashStep();

            if (steps.Steps.Count > 2) {
                asset.Status = MediaAssetStatus.QUEUED;
                await _MediaAssetRepository.Upsert(asset);

                _Logger.LogInformation($"queueing for processing [MediaAssetID={steps.Asset.Guid}]");
                _UploadStepsQueue.Queue(steps);
            } else {
                _Logger.LogInformation($"running final move step right away [MediaAssetID={steps.Asset.Guid}]");
                await _UploadStepsHandler.Run(steps, CancellationToken.None);

                asset = await _MediaAssetRepository.GetByID(steps.Asset.Guid)
                    ?? throw new Exception($"missing {nameof(MediaAsset)} {steps.Asset.Guid} when expected to exist");
            }

            _Logger.LogDebug($"all done! [steps.Count={steps.Steps.Count}]");

            return ApiOk(asset);
        }

        /// <summary>
        ///     regenerate the <see cref="MediaAsset.IqdbHash"/>
        /// </summary>
        /// <param name="assetID">ID of the <see cref="MediaAsset"/> to regenerate the IQDB hash of</param>
        /// <response code="200">
        ///     the <see cref="MediaAsset.IqdbHash"/> field of the <see cref="MediaAsset"/>
        ///     with <see cref="MediaAsset.Guid"/> of <paramref name="assetID"/> was successfully updated,
        ///     and included in the returned response
        /// </response>
        /// <response code="404">
        ///     no <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="assetID"/> exists
        /// </response>
        /// <response code="500">
        ///     the IQDB service is not currently available
        /// </response>
        [HttpPost("{assetID}/regenerate-iqdb")]
        public async Task<ApiResponse<IqdbEntry>> RegenerateIqdbHash(Guid assetID) {
            MediaAsset? asset = await _MediaAssetRepository.GetByID(assetID);
            if (asset == null) {
                return ApiNotFound<IqdbEntry>($"{nameof(MediaAsset)} {assetID}");
            }

            if (asset.Status != MediaAssetStatus.DONE) {
                return ApiBadRequest<IqdbEntry>($"{nameof(MediaAsset)} {assetID} is not {MediaAssetStatus.DONE}, is {asset.Status} instead");
            }

            ServiceHealthEntry health = await _IqdbClient.CheckHealth();
            if (health.Enabled == false) {
                return ApiInternalError<IqdbEntry>($"IQDB client cannot handle requests currently: {health.Message}");
            }

            string path = Path.Combine(_StorageOptions.Value.RootDirectory, "original", asset.FileLocation);
            _Logger.LogDebug($"regenerated IQDB hash [assetID={assetID}] [path={path}]");

            await _IqdbClient.RemoveByMD5(asset.MD5);
            IqdbEntry? entry = await _IqdbClient.Create(path, asset.MD5, asset.FileExtension);

            if (entry == null) {
                return ApiInternalError<IqdbEntry>($"failed to generate IQDB hash, please check console logs");
            }

            _Logger.LogInformation($"regenerated hash for media asset [assetID={assetID}] [hash={entry.Hash}]");

            asset.IqdbHash = entry.Hash;
            await _MediaAssetRepository.Upsert(asset);

            return ApiOk(entry);
        }

        /// <summary>
        ///     delete a media asset
        /// </summary>
        /// <param name="assetID">GUID of the media asset to delete</param>
        /// <response code="200">
        ///     the <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="assetID"/>
        ///     was successfully deleted, and removed from the IQDB service
        /// </response>
        /// <response code="404">
        ///     no <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="assetID"/> exists
        /// </response>
        [HttpDelete("{assetID}")]
        public async Task<ApiResponse> Delete(Guid assetID) {
            MediaAsset? asset = await _MediaAssetRepository.GetByID(assetID);
            if (asset == null) {
                return ApiNotFound($"{nameof(MediaAsset)} {assetID}");
            }

            if (asset.IqdbHash != null && asset.IqdbHash != "") {
                await _IqdbClient.RemoveByMD5(asset.MD5);
            }

            await _MediaAssetRepository.Erase(assetID);

            return ApiOk();
        }

    }
}
