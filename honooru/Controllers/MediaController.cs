using honooru.Code;
using honooru.Models.Config;
using honooru.Models.Internal;
using honooru.Services.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace honooru.Controllers {

    public class MediaController : Controller {

        private readonly ILogger<MediaController> _Logger;
        private readonly IOptions<StorageOptions> _Options;

        private const string THUMBNAIL_SIZE = "180x180";

        private readonly FileExtensionService _FileExtensionHelper;

        public MediaController(ILogger<MediaController> logger, IOptions<StorageOptions> options,
            FileExtensionService fileExtensionHelper) {

            _Logger = logger;
            _Options = options;

            _FileExtensionHelper = fileExtensionHelper;
        }

        //[PermissionNeeded(AppPermission.APP_VIEW)]
        [AllowAnonymous]
        public IActionResult Get() {
            string? urlPath = Request.Path.Value;
            if (urlPath == null) {
                return BadRequest("no path supplied");
            }

            // /media/180x180/MD5.{ext}
            string[] parts = urlPath.Split("/");

            if (parts.Length < 4) {
                return BadRequest($"expected 4 parts from {urlPath}, got {parts.Length} instead");
            }

            string size = parts[2];
            string md5 = parts[3]; // this include the file extension

            string[] extParts = md5.Split(".");
            if (extParts.Length != 2) {
                return BadRequest($"failed to split {md5} into a file name and extention (got {extParts.Length} instead of 2)");
            }
            string fileExt = extParts[1];

            string path = Path.Combine(_Options.Value.RootDirectory, size, md5[..2], md5);

            //_Logger.LogDebug($"loading media [file path={path}] [size={size}] [md5={md5}] [Path={string.Join(" ", parts)}]");

            if (System.IO.File.Exists(path) == false) {
                return NotFound();
            }

            // DO NOT use |using| here, as calling File() will dispose of the stream for us
            FileStream file = System.IO.File.OpenRead(path);

            string contentType = _FileExtensionHelper.GetContentType(fileExt);
            //_Logger.LogDebug($"sending physical file path [md5={md5}] [fileExt={fileExt}] [Content-Type={contentType}]");

            // enable range processing is what allows seeking within long videos
            return File(file, contentType, enableRangeProcessing: true);
        }

    }
}
