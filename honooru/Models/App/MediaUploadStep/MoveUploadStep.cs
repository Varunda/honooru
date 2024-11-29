using honooru.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Models.App.MediaUploadStep {

    public static class MoveUploadStep {

        public class Order : UploadStep, IUploadStep<Order, Worker> {

            public string Name => "copy to /original";

            public Order(Guid assetID, StorageOptions options) : base(assetID, options) { }

        }

        public class Worker : IUploadStepWorker<Order, Worker> {

            private readonly ILogger<Worker> _Logger;

            public Worker(ILogger<Worker> logger) {
                _Logger = logger;
            }

            public Task<bool> Run(Order order, MediaAsset asset, Action<decimal> progressCallback, CancellationToken cancel) {
                string input = Path.Combine(order.StorageOptions.RootDirectory, "work", asset.MD5 + "." + asset.FileExtension);
                string output = Path.Combine(order.StorageOptions.RootDirectory, "original", asset.FileLocation);
                Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "");

                _Logger.LogInformation($"copying media asset [input={input}] [output={output}]");

                if (File.Exists(input) == false) {
                    throw new Exception($"input file '{input}' does not exist");
                }

                if (File.Exists(output) == true) {
                    return Task.FromResult(true);
                }

                progressCallback(0m);
                File.Move(input, output);
                progressCallback(100m);
                return Task.FromResult(true);
            }

        }

    }
}
