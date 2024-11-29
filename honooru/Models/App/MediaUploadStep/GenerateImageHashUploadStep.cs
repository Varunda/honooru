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

            public Order(Guid assetID, StorageOptions options) : base(assetID, options) { }

        }

        public class Worker : IUploadStepWorker<Order, Worker> {

            private readonly ILogger<Worker> _Logger;
            private readonly IqdbClient _Iqdb;

            public Worker(ILogger<Worker> logger, IqdbClient iqdb) {
                _Logger = logger;
                _Iqdb = iqdb;
            }

            public async Task<bool> Run(Order order, MediaAsset asset, Action<decimal> progressCallback, CancellationToken cancel) {
                progressCallback(0m);

                _Logger.LogDebug($"generating image hash [md5={asset.MD5}] [fileExtension={asset.FileExtension}]");
                string input = Path.Combine(order.StorageOptions.RootDirectory, "original", asset.FileLocation);

                if ((await _Iqdb.CheckHealth()).Enabled == false) {
                    _Logger.LogWarning($"IQDB service is not enabled, skipping IQDB hash set [md5={asset.MD5}]");
                    asset.IqdbHash = "";
                } else {
                    IqdbEntry? response = await _Iqdb.Create(input, asset.MD5, asset.FileExtension);

                    asset.IqdbHash = response?.Hash;
                }

                progressCallback(100m);

                return true;
            }

        }

    }
}
