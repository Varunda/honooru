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

        [PermissionNeeded(AppPermission.APP_VIEW)]
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
            string md5 = parts[3];

            string[] extParts = md5.Split(".");
            if (extParts.Length != 2) {
                return BadRequest($"needed 2 '.'s from {md5}");
            }
            string fileExt = extParts[1];

            string path = Path.Combine(_Options.Value.RootDirectory, size, md5);

            //_Logger.LogDebug($"loading media [file path={path}] [size={size}] [md5={md5}] [Path={string.Join(" ", parts)}]");

            if (System.IO.File.Exists(path) == false) {
                return NotFound();
            }

            string contentType = _FileExtensionHelper.GetContentType(fileExt);
            //_Logger.LogDebug($"sending physical file path [md5={md5}] [fileExt={fileExt}] [Content-Type={contentType}]");

            return PhysicalFile(path, contentType);

        }

    }
}
