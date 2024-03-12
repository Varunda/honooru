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

            public Order(MediaAsset asset, StorageOptions options) : base(asset, options) { }

        }

        public class Worker : IUploadStepWorker<Order, Worker> {

            private readonly ILogger<Worker> _Logger;

            public Worker(ILogger<Worker> logger) {
                _Logger = logger;
            }

            public Task Run(Order order, Action<decimal> progressCallback, CancellationToken cancel) {
                string input = Path.Combine(order.StorageOptions.RootDirectory, "work", order.Asset.MD5 + "." + order.Asset.FileExtension);
                string output = Path.Combine(order.StorageOptions.RootDirectory, "original", order.Asset.MD5 + "." + order.Asset.FileExtension);

                _Logger.LogInformation($"copying media asset [input={input}] [output={output}]");

                if (File.Exists(input) == false) {
                    throw new Exception($"input file '{input}' does not exist");
                }

                if (File.Exists(output) == true) {
                    return Task.CompletedTask;
                }

                progressCallback(0m);
                File.Move(input, output);
                progressCallback(1m);
                return Task.CompletedTask;
            }

        }

    }
}
