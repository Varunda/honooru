using honooru.Models.Config;
using honooru.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Models.App.MediaUploadStep {

    public static class GenerateImageHashUploadStep {

        public class Order : UploadStep, IUploadStep<Order, Worker> {

            public string Name => "generate image hash";

            public Order(MediaAsset asset, StorageOptions options) : base(asset, options) { }

        }

        public class Worker : IUploadStepWorker<Order, Worker> {

            private readonly ILogger<Worker> _Logger;
            private readonly IqdbClient _Iqdb;

            public Worker(ILogger<Worker> logger, IqdbClient iqdb) {
                _Logger = logger;
                _Iqdb = iqdb;
            }

            public async Task<bool> Run(Order order, Action<decimal> progressCallback, CancellationToken cancel) {
                progressCallback(0m);

                _Logger.LogDebug($"generating image hash [md5={order.Asset.MD5}] [fileExtension={order.Asset.FileExtension}]");
                string input = Path.Combine(order.StorageOptions.RootDirectory, "original", order.Asset.FileLocation);

                IqdbEntry? response = await _Iqdb.Create(input, order.Asset.MD5, order.Asset.FileExtension);

                order.Asset.IqdbHash = response?.Hash;

                progressCallback(100m);

                return true;
            }

        }

    }
}
