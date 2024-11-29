using Grpc.Core;
using honooru.Models.App;
using honooru.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace honooru.Services.UrlMediaExtrator {

    public class StaticFileExtractor : IUrlMediaExtractor {

        public string Name => "static_file";

        public bool NeedsQueue => false;

        private readonly ILogger<StaticFileExtractor> _Logger;

        private static readonly HttpClient _HttpClient = new HttpClient();

        public StaticFileExtractor(ILogger<StaticFileExtractor> logger) {
            _Logger = logger;
        }

        public bool CanHandle(Uri url) {
            return url.AbsolutePath.EndsWith(".png")
                || url.AbsolutePath.EndsWith(".jpg")
                || url.AbsolutePath.EndsWith(".jpeg")
                || url.AbsolutePath.EndsWith(".mp4")
                || url.AbsolutePath.EndsWith(".webp")
                || url.AbsolutePath.EndsWith(".webm");
        }

        public async Task<MediaAsset> Handle(Uri url, StorageOptions options, MediaAsset asset, Action<decimal> progress) {
            _Logger.LogInformation($"performing static file extraction [url={url}]");

            progress(0m);
            HttpResponseMessage response = await _HttpClient.GetAsync(url);
            progress(100m);

            _Logger.LogDebug($"static file request [status={response.StatusCode}] [url={url}]");

            if (!response.IsSuccessStatusCode) {
                _Logger.LogError($"failed to get static file [status={response.StatusCode}] [url={url}]");
                return asset;
            }

            string fileName = url.AbsolutePath.Split("/")[^1];
            if (fileName.Split(".").Length < 2) {
                _Logger.LogWarning($"failed to split '{fileName}' into 2 parts (based on '.'), missing file extension!");
            }
            string fileExtension = fileName.Split(".")[^1];

            _Logger.LogDebug($"saving file to upload directory [fileName={fileName}] [fileExtension={fileExtension}]");
            string path = Path.Combine(options.RootDirectory, "upload", asset.Guid + "." + fileExtension);
            _Logger.LogInformation($"saving asset to storage [fileName={fileName}] [fileExtension={fileExtension}] [path={path}]");

            using FileStream file = File.OpenWrite(path);
            using Stream contentStream = await response.Content.ReadAsStreamAsync();
            await contentStream.CopyToAsync(file);

            asset.FileExtension = fileExtension;
            asset.FileName = fileName;
            asset.FileSizeBytes = file.Position;

            return asset;
        }

    }
}
