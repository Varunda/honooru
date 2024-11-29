using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.Config;
using honooru.Models.Db;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Repositories;
using honooru.Services.Util;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Controllers.Api.Upload {

    /// <summary>
    ///     a common controller that both <see cref="MediaAssetApiController"/> and <see cref="DistributedJobApiController"/> use
    ///     to handle asset uploads
    /// </summary>
    public class FileUploadBaseController : ApiControllerBase {

        internal readonly ILogger<FileUploadBaseController> _Logger;
        internal readonly AppCurrentAccount _CurrentUser;

        internal readonly PostDb _PostDb;
        internal readonly MediaAssetRepository _MediaAssetRepository;
        internal readonly FileExtensionService _FileExtensionHelper;
        internal readonly IOptions<StorageOptions> _StorageOptions;

        public FileUploadBaseController(ILoggerFactory loggerFactory,
            AppCurrentAccount currentUser, MediaAssetRepository mediaAssetRepository,
            FileExtensionService fileExtensionHelper, IOptions<StorageOptions> storageOptions,
            PostDb postDb) {

            _Logger = loggerFactory.CreateLogger<FileUploadBaseController>();

            _CurrentUser = currentUser;
            _MediaAssetRepository = mediaAssetRepository;
            _FileExtensionHelper = fileExtensionHelper;
            _StorageOptions = storageOptions;
            _PostDb = postDb;
        }

        /// <summary>
        ///     this method will:
        ///     <ul>
        ///         <li>compute the hash of the <see cref="MediaAsset"/></li>
        ///         <li>ensure no existing post exists that matches the MD5</li>
        ///         <li>ensure no existing media asset (other than the one passed) matches the MD5</li>
        ///         <li>moves it from the /upload to /work folder</li>
        ///         <li>and updates all the fields of the media asset</li>
        ///     </ul>
        ///     these steps ensure the media asset is ready to be processed (such as an mkv being re-encoded to mp4)
        /// </summary>
        /// <param name="asset">asset to be handled</param>
        /// <param name="moveAsset">is the assset moved to /work from /upload?</param>
        /// <returns></returns>
        internal async Task<ApiResponse<MediaAsset>> _HandleAsset(MediaAsset asset, bool moveAsset = true) {

            string filePath = Path.Combine(_StorageOptions.Value.RootDirectory, "upload", asset.Guid + "." + asset.FileExtension);
            FileStreamOptions opt = new() {
                BufferSize = 1024 * 1024 * 128, // 128 MB buffer instead of default 4'096
                Mode = FileMode.Open,
                Access = FileAccess.Read
            };
            using FileStream targetFile = System.IO.File.Open(filePath, opt);

            // IMPORTANT: move back to the start of the file so the md5 hashing uses the whole file stream
            targetFile.Seek(0, SeekOrigin.Begin);
            byte[] md5 = await MD5.Create().ComputeHashAsync(targetFile);
            asset.FileSizeBytes = targetFile.Position; // the position is currently at the end file, or the whole length of the file
            targetFile.Close(); // IMPORTANT: if this is not closed, the handle is left open, and the file cannot be moved
            string md5Str = md5.JoinToString();
            _Logger.LogInformation($"computed hash [md5={md5Str}] [filePath={filePath}]");

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
            if (existingAsset != null && existingAsset.Guid != asset.Guid) {
                _Logger.LogDebug($"media asset already pending, returning that one instead [md5Str={md5Str}] [existingAsset={existingAsset.Guid}] [asset.Guid={asset.Guid}]");
                return ApiOk(existingAsset);
            }

            asset.MD5 = md5Str;
            asset.Timestamp = DateTime.UtcNow;
            asset.Status = MediaAssetStatus.PROCESSING;

            if (moveAsset == true) {
                // files are uploaded with a random GUID, then moved to with a name based on the MD5 hash instead
                string moveInputPath = Path.Combine(_StorageOptions.Value.RootDirectory, "upload", asset.Guid + "." + asset.FileExtension);
                string moveOutputPath = Path.Combine(_StorageOptions.Value.RootDirectory, "work", asset.MD5 + "." + asset.FileExtension);
                _Logger.LogInformation($"moving uploaded file to work directory [input={moveInputPath}] [output={moveOutputPath}");
                if (System.IO.File.Exists(moveOutputPath) == false) {
                    System.IO.File.Move(moveInputPath, moveOutputPath);
                } else {
                    _Logger.LogDebug($"move already complete [input={moveInputPath}] [output={moveOutputPath}]");
                }
            }

            await _MediaAssetRepository.Upsert(asset);

            return ApiOk(asset);
        }

        /// <summary>
        ///     read a section from a request and append it to the file in /upload.
        ///     it is expected the file being appended to is good to be appended to. this lets this method be
        ///     used for both uploads that take place in one HTTP call, and uploads that are chunked
        /// </summary>
        /// <param name="uploadId">ID of the <see cref="MediaAsset"/> the section read will be appended to</param>
        /// <param name="expectedStatus">the expected status of the <see cref="MediaAsset"/></param>
        /// <returns></returns>
        internal async Task<ApiResponse<MediaAsset>> ReadSection(Guid uploadId, MediaAssetStatus expectedStatus) {
            MediaAsset? asset = await _MediaAssetRepository.GetByID(uploadId);
            if (asset == null) {
                return ApiNotFound<MediaAsset>($"{nameof(MediaAsset)} {uploadId}");
            }

            if (asset.Status != expectedStatus) {
                return ApiBadRequest<MediaAsset>($"expected {nameof(MediaAsset)} {uploadId} to be {expectedStatus}, but it was {asset.Status} instead");
            }

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
            _Logger.LogDebug($"originalName={originalName}");
            asset.FileName = WebUtility.HtmlEncode(originalName) ?? "";

            string? extension = HeaderUtilities.RemoveQuotes(Path.GetExtension(originalName)).Value;
            if (string.IsNullOrWhiteSpace(extension)) {
                return ApiBadRequest<MediaAsset>($"extension from name {originalName} is null or empty");
            }

            extension = extension[1..].ToLower(); // remove the leading .

            if (_FileExtensionHelper.IsValid(extension) == false) {
                _Logger.LogWarning($"disallowing invalid extension [extension={extension}] [originalName={originalName}]");
                return ApiBadRequest<MediaAsset>($"invalid extension '{extension}' (invalid)");
            }

            if (asset.AdditionalTags == "") {
                string? fileType = _FileExtensionHelper.GetFileType(extension);
                if (fileType == null) {
                    _Logger.LogWarning($"failed to get file type from extension [extension={extension}]");
                    return ApiBadRequest<MediaAsset>($"invalid extension '{extension}' (failed to get file type)");
                } else if (fileType == "image") {
                    if (extension != "gif") {
                        asset.AdditionalTags += " image";
                    } else {
                        asset.AdditionalTags += " animated";
                    }
                } else if (fileType == "video") {
                    asset.AdditionalTags += " video animated";
                }
                _Logger.LogDebug($"extension found [originalName={originalName}] [extension={extension}] [fileType={fileType}]");
            }

            asset.FileExtension = extension;
            await _MediaAssetRepository.Upsert(asset);

            string filePath = Path.Combine(_StorageOptions.Value.RootDirectory, "upload", uploadId + "." + asset.FileExtension);
            _Logger.LogInformation($"writing new upload [filePath={filePath}]");

            FileStreamOptions opt = new() {
                BufferSize = 1024 * 1024 * 128, // 128 MB buffer instead of default 4'096
                Mode = FileMode.Append, // append to the end of the file
                Access = FileAccess.Write
            };
            using FileStream targetFile = System.IO.File.Open(filePath, opt);
            await part.Body.CopyToAsync(targetFile);
            
            part = await reader.ReadNextSectionAsync();
            if (part != null) {
                _Logger.LogInformation($"expected part to be null here?");
            }

            return ApiOk(asset);
        }

        internal static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                    || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }


    }
}
