using honooru.Models.Config;
using ImageMagick;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Models.App.MediaUploadStep {

    public static class PdfUploadStep {

        public class Order : UploadStep, IUploadStep<Order, Worker> {

            public string Name => "turn a pdf into an image";

            public Order(Guid assetID, StorageOptions options) : base(assetID, options) { }

        }

        public class Worker : IUploadStepWorker<Order, Worker> {

            private readonly ILogger<Worker> _Logger;

            public Worker(ILogger<Worker> logger) {
                _Logger = logger;
            }

            public async Task<bool> Run(Order order, MediaAsset asset, Action<decimal> progressCallback, CancellationToken cancel) {
                cancel.Register(() => {
                    _Logger.LogWarning($"cancellation token called! [asset={asset.Guid}]");
                });

                string input = Path.Combine(order.StorageOptions.RootDirectory, "work", asset.MD5 + "." + asset.FileExtension);

                string outputDir = Path.Combine(order.StorageOptions.RootDirectory, "work", asset.MD5);
                if (Directory.Exists(outputDir) == true) {
                    _Logger.LogDebug($"outputDir for PDF conversion already exists [outputDir={outputDir}] [md5={asset.MD5}] [asset={asset.Guid}]");
                } else {
                    DirectoryInfo info = Directory.CreateDirectory(outputDir);
                }

                string output = Path.Combine(order.StorageOptions.RootDirectory, "work", asset.MD5 + ".png");

                _Logger.LogInformation($"converting pdf to png [fileExt={asset.FileExtension}] [input={input}] [output={output}]");

                if (File.Exists(output) == true) {
                    _Logger.LogInformation($"converted file already exists, skipping [output={output}] [md5={asset.MD5}]");
                    return true;
                }

                if (File.Exists(input) == false) {
                    throw new Exception($"input file '{input}' does not exist");
                }

                MagickReadSettings mRead = new() {
                    Density = new Density(300)
                };

                using MagickImageCollection pdfInput = new();
                _Logger.LogDebug($"reading pdf into image collection [input={input}] [asset={asset.Guid}] [md5={asset.MD5}]");
                progressCallback(25m);
                await pdfInput.ReadAsync(input, mRead, cancel);
                progressCallback(50m);
                _Logger.LogDebug($"pdf read, appending vertically [input={input}] [asset={asset.Guid}] [md5={asset.MD5}]");

                using IMagickImage<ushort> pdfImage = pdfInput.AppendVertically();
                progressCallback(75m);
                _Logger.LogDebug($"created image stream, writing to output [output={output}] [asset={asset.Guid}] [md5={asset.MD5}]");
                await pdfImage.WriteAsync(output, cancel);
                progressCallback(90m);
                _Logger.LogDebug($"created image [output={output}] [asset={asset.Guid}] [md5={asset.MD5}]");

                if (File.Exists(output) == false) {
                    _Logger.LogError($"why does the output file not exist ??? [output={output}]");
                }

                _Logger.LogInformation($"deleting old file [input={input}]");
                File.Delete(input);
                _Logger.LogDebug($"file deleted [input={input}]");

                _Logger.LogDebug($"creating FileInfo [output={output}]");
                asset.FileSizeBytes = new FileInfo(output).Length;
                asset.FileExtension = "png";

                progressCallback(100m);

                return true;
            }

        }

    }
}
