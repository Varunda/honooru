using honooru.Models;
using honooru.Models.App;
using honooru.Models.Config;
using honooru.Models.Db;
using honooru.Services;
using honooru.Services.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [Route("/api/media-asset")]
    [ApiController]
    public class MediaAssetApiController : ApiControllerBase {

        private readonly ILogger<MediaAssetApiController> _Logger;

        private readonly IOptions<StorageOptions> _StorageOptions;

        private readonly AppCurrentAccount _CurrentAccount;
        private readonly PostDb _PostDb;
        private readonly MediaAssetDb _MediaAssetDb;

        public MediaAssetApiController(ILogger<MediaAssetApiController> logger,
            IOptions<StorageOptions> storageOptions,
            PostDb postDb, AppCurrentAccount currentAccount,
            MediaAssetDb mediaAssetDb) {

            _Logger = logger;

            _StorageOptions = storageOptions;

            _PostDb = postDb;
            _CurrentAccount = currentAccount;
            _MediaAssetDb = mediaAssetDb;
        }

        /// <summary>
        ///     get a <see cref="MediaAsset"/> by its <see cref="MediaAsset.ID"/>
        /// </summary>
        /// <param name="assetID">ID of the <see cref="MediaAsset"/> to get</param>
        /// <response code="200">
        ///     the response will contain the <see cref="MediaAsset"/> with
        ///     <see cref="MediaAsset.ID"/> of <paramref name="assetID"/>
        /// </response>
        /// <response code="204">
        ///     no <see cref="MediaAsset"/> with <see cref="MediaAsset.ID"/> of <paramref name="assetID"/> exists
        /// </response>
        [HttpGet("{assetID}")]
        public async Task<ApiResponse<MediaAsset>> GetByID(ulong assetID) {
            MediaAsset? asset = await _MediaAssetDb.GetByID(assetID);

            if (asset == null) {
                return ApiNoContent<MediaAsset>();
            }

            return ApiOk(asset);
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
        public async Task<ApiResponse<MediaAsset>> Upload() {
            string contentType = Request.ContentType ?? "";
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase)) {
                return ApiBadRequest<MediaAsset>($"ContentType '{contentType}' is not a multipart-upload");
            }

            MediaTypeHeaderValue type = MediaTypeHeaderValue.Parse(Request.ContentType);
            string boundary = HeaderUtilities.RemoveQuotes(type.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary)) {
                return ApiBadRequest<MediaAsset>($"boundary from ContentType '{Request.ContentType}' was is null or empty ({type}) ({type.Boundary})");
            }

            MediaAsset asset = new();

            string originalName = "";
            string safeName = "";
            byte[] file = Array.Empty<byte>();

            MultipartReader reader = new(boundary, Request.Body);
            MultipartSection? part = await reader.ReadNextSectionAsync();

            while (part != null) {
                bool hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(part.ContentDisposition, out ContentDispositionHeaderValue? contentDisposition);
                if (hasContentDispositionHeader) {
                    if (contentDisposition == null) {
                        return ApiInternalError<MediaAsset>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
                    }

                    if (HasFileContentDisposition(contentDisposition)) {
                        originalName = contentDisposition.FileName.Value;
                        safeName = WebUtility.HtmlEncode(originalName);
                        asset.FileName = safeName;

                        string? extension = HeaderUtilities.RemoveQuotes(Path.GetExtension(originalName)).Value;
                        if (string.IsNullOrWhiteSpace(extension)) {
                            return ApiBadRequest<MediaAsset>($"extension from name {originalName} is null or empty");
                        }

                        // a valid extension can only have one '.', and can only be alphanumeric
                        if (extension.Any(iter => !char.IsLetterOrDigit(iter) && iter != '.')
                                || extension.Count(iter => iter == '.') > 1) {

                            _Logger.LogWarning($"disallowing invalid extension [extension={extension}] [originalName={originalName}]");
                            return ApiBadRequest<MediaAsset>($"invalid extension '{extension}'");
                        }

                        _Logger.LogDebug($"extension found [originalName={originalName}] [extension={extension}]");

                        using MemoryStream stream = new();
                        await part.Body.CopyToAsync(stream);

                        if (stream.Length == 0) {
                            _Logger.LogError($"file is of length 0");
                        }

                        file = stream.ToArray();

                        byte[] md5 = MD5.Create().ComputeHash(file);
                        string md5Str = string.Join("", md5.Select(iter => iter.ToString("x2")));
                        asset.MD5 = md5Str;
                        _Logger.LogInformation($"hash of file: {md5Str}");

                        // if a post with the md5 of this upload already exists, don't proceed
                        Post? existingPost = await _PostDb.GetByMD5(asset.MD5);
                        if (existingPost != null) {
                            return ApiRedirectFound<MediaAsset>($"/post/{existingPost.ID}");
                        }

                        // if a media asset with the md5 of the upload already exists, don't make a new one
                        MediaAsset? existingAsset = await _MediaAssetDb.GetByMD5(asset.MD5);
                        if (existingAsset != null) {
                            return ApiRedirectFound<MediaAsset>($"/media_asset/{existingAsset.ID}");
                        }

                        // extension includes the '.' already
                        string filePath = Path.Combine(_StorageOptions.Value.RootDirectory, md5Str + extension);
                        asset.FileLocation = filePath;

                        if (System.IO.File.Exists(filePath)) {
                            _Logger.LogInformation($"file {filePath} for {originalName} already exists");
                        } else {
                            using var targetFile = System.IO.File.Create(filePath);
                            await targetFile.WriteAsync(file);

                            _Logger.LogInformation($"wrote {targetFile.Position} bytes to {filePath}");
                            asset.FileSizeBytes = targetFile.Position;
                            asset.Timestamp = DateTime.UtcNow;
                            asset.ID = await _MediaAssetDb.Insert(asset);
                        }
                    } else {
                        _Logger.LogError($"not a file content disposition");
                    }
                }

                part = await reader.ReadNextSectionAsync();

                if (part != null) {
                    _Logger.LogInformation($"expected part to be null here?");
                }
            }

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
