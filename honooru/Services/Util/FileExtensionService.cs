using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace honooru.Services.Util {

    public class FileExtensionService {

        private readonly ILogger<FileExtensionService> _Logger;

        private static readonly Dictionary<string, string> _ContentTypeMappings = new() {
            { "apng", "image/apng" },
            { "avif", "image/avif" },
            { "avi", "video/x-msvideo" },
            { "bmp", "image/bmp" },
            { "gif", "image/gif" },
            { "ico", "image/vnd.microsoft.icon" },
            { "jpeg", "image/jpeg" },
            { "jpg", "image/jpeg" },
            { "mkv", "video/x-matroska" },
            { "m4v", "video/x-m4v" },
            { "mp3", "audio/mpeg" },
            { "mp4", "video/mp4" },
            { "mpeg", "image/mpeg" },
            { "opsu", "audio/opus" },
            { "png", "image/png" },
            { "svg", "image/svg+xml" },
            { "wav", "audio/wav" },
        };

        private static readonly Dictionary<string, string> _FileTypeMappings = new() {
            // image
            { "jpg", "image" },
            { "jpeg", "image" },
            { "png", "image" },

            // video
            { "mp4", "video" },
            { "mkv", "video" },
            { "m4v", "video" },
            { "webm", "video" },
        };

        public FileExtensionService(ILogger<FileExtensionService> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     check if an extension is valid or not
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public bool IsValid(string extension) {
            // a valid extension can only have one '.', and can only be alphanumeric
            return !(extension.Any(iter => !char.IsLetterOrDigit(iter) && iter != '.')
                    || extension.Count(iter => iter == '.') > 1);
        }

        public string? GetFileType(string extension) {
            if (extension.Length == 0) {
                return null;
            }

            if (extension[0] == '.') {
                extension = extension[1..];
            }

            if (extension.Length == 0) {
                return null;
            }

            _Logger.LogDebug($"getting file type [extension={extension}]");

            return _FileTypeMappings.GetValueOrDefault(extension);
        }

        public string GetContentType(string extension) {
            if (extension.Length == 0) {
                return "application/octet-stream";
            }

            if (extension[0] == '.') {
                extension = extension[1..];
            }

            if (extension.Length == 0) {
                return "application/octet-stream";
            }

            return _ContentTypeMappings.GetValueOrDefault(extension) ?? "application/octet-stream";
        }

    }
}
