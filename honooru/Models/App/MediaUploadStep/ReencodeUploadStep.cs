using FFMpegCore;
using FFMpegCore.Enums;
using honooru.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Models.App.MediaUploadStep {

    public static class ReencodeUploadStep {

        public class Order : UploadStep, IUploadStep<Order, Worker> {

            public string Name => "reencode to mp4";

            public Order(MediaAsset asset, StorageOptions options, Codec videoFormat, Codec audioCodec) : base(asset, options) {
                VideoFormat = videoFormat;
                AudioCodec = audioCodec;
            }

            public readonly Codec VideoFormat;

            public readonly Codec AudioCodec;

        }

        public class Worker : IUploadStepWorker<Order, Worker> {

            private readonly ILogger<Worker> _Logger;

            public Worker(ILogger<Worker> logger) {
                _Logger = logger;
            }

            public async Task Run(Order order, Action<decimal> progressCallback, CancellationToken cancel) {
                string input = Path.Combine(order.StorageOptions.RootDirectory, "work", order.Asset.MD5 + "." + order.Asset.FileExtension);
                string output = Path.Combine(order.StorageOptions.RootDirectory, "work", order.Asset.MD5 + ".mp4");

                _Logger.LogInformation($"performing re-encode work [videoFormat={order.VideoFormat.Name}] [audioFormat={order.AudioCodec.Name}] [input={input}] [output={output}]");

                if (File.Exists(input) == false) {
                    throw new Exception($"input file '{input}' does not exist");
                }

                IMediaAnalysis analysis = await FFProbe.AnalyseAsync(input, null, cancel);
                TimeSpan videoDuration = analysis.Duration;
                _Logger.LogDebug($"input file length: {videoDuration.TotalSeconds} seconds [input={input}]");

                await FFMpegArguments.FromFileInput(input)
                    .OutputToFile(output, false, (options) => {
                        options
                            .WithVideoCodec(VideoCodec.LibX264)
                            .WithAudioCodec(AudioCodec.Aac)
                            .ForceFormat("mp4")
                            .WithFastStart();
                    })
                    .NotifyOnProgress((double arg0) => {
                        progressCallback((decimal) arg0);
                    }, videoDuration)
                    .ProcessAsynchronously();

                if (File.Exists(output) == false) {
                    _Logger.LogError($"why does the output file not exist ??? [output={output}]");
                }

                _Logger.LogInformation($"deleting old file [input={input}]");
                File.Delete(input);
                _Logger.LogDebug($"file deleted [input={input}]");

                _Logger.LogDebug($"creating FileInfo [output={output}]");
                order.Asset.FileSizeBytes = new FileInfo(output).Length;
                order.Asset.FileExtension = "mp4";
            }

        }


    }

}
