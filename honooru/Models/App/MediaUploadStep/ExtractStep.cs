using FFMpegCore.Enums;
using honooru.Models.Config;
using honooru.Services.UrlMediaExtrator;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Models.App.MediaUploadStep {

    public static class ExtractStep {

        public class Order : UploadStep, IUploadStep<Order, Worker> {

            public string Name => "extract";

            public string Url { get; set; }

            public Order(MediaAsset asset, StorageOptions options, string url) : base(asset, options) {
                Url = url;
            }

        }

        public class Worker : IUploadStepWorker<Order, Worker> {

            private readonly ILogger<Worker> _Logger;
            private readonly UrlMediaExtractorHandler _UrlExtractor;

            public Worker(ILogger<Worker> logger,
                UrlMediaExtractorHandler urlExtractor) {

                _Logger = logger;

                _UrlExtractor = urlExtractor;
            }

            public async Task<bool> Run(Order order, Action<decimal> updateProgress, CancellationToken cancel) {
                _Logger.LogInformation($"extracting from url [url={order.Url}]");

                if (_UrlExtractor.CanHandle(order.Url) == false) {
                    throw new Exception($"cannot handle url '{order.Url}', url extractor cannot handle it");
                }

                updateProgress(0m);
                MediaAsset a = await _UrlExtractor.HandleUrl(order.Asset, order.Url, updateProgress);
                updateProgress(100m);

                if (a.Guid != order.Asset.Guid) {
                    _Logger.LogInformation($"detected change in extracted asset, Guid changed [input={order.Asset.Guid}] [output={a.Guid}");
                    return false;
                }

                return true;
            }

        }

    }
}
