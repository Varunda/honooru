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
            return url.AbsolutePath.EndsWith(".png");
        }

        public async Task Handle(Uri url, StorageOptions options, MediaAsset asset, Action<decimal> progress) {
            _Logger.LogInformation($"performing static file extraction [url={url}]");

            progress(0);
            HttpResponseMessage response = await _HttpClient.GetAsync(url);
            progress(1);

            _Logger.LogDebug($"static file request [status={response.StatusCode}] [url={url}]");

            if (!response.IsSuccessStatusCode) {
                _Logger.LogError($"failed to get static file [statis={response.StatusCode}] [url={url}]");
                return;
            }

            string path = Path.Combine(options.RootDirectory, "upload", asset.Guid + ".png");
            _Logger.LogInformation($"saving asset to storage [path={path}]");

            using FileStream file = File.OpenWrite(path);
            using Stream contentStream = await response.Content.ReadAsStreamAsync();
            await contentStream.CopyToAsync(file);

            asset.FileExtension = "png";
            asset.FileSizeBytes = file.Position;
        }

    }
}
