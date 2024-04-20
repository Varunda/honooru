using FFMpegCore;
using FFMpegCore.Enums;
using honooru.Models.Config;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
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

            public async Task<bool> Run(Order order, Action<decimal> progressCallback, CancellationToken cancel) {
                if (order.Asset.FileExtension == "mp4" || order.Asset.FileExtension == "webm") {
                    _Logger.LogDebug($"skipping re-encoding of file that is already web playable [FileExtension={order.Asset.FileExtension}] [id={order.Asset.Guid}] [md5={order.Asset.MD5}]");
                    progressCallback(100m);
                    return true;
                }

                cancel.Register(() => {
                    _Logger.LogWarning($"cancellation token called! [order.Asset={order.Asset.Guid}]");
                });

                string input = Path.Combine(order.StorageOptions.RootDirectory, "work", order.Asset.MD5 + "." + order.Asset.FileExtension);
                string output = Path.Combine(order.StorageOptions.RootDirectory, "work", order.Asset.MD5 + ".mp4");

                _Logger.LogInformation($"performing re-encode work [videoFormat={order.VideoFormat.Name}] [audioFormat={order.AudioCodec.Name}] [input={input}] [output={output}]");

                if (File.Exists(output) == true) {
                    _Logger.LogInformation($"re-encoded file already exists, skipping [output={output}] [md5={order.Asset.MD5}]");
                    return true;
                }

                if (File.Exists(input) == false) {
                    throw new Exception($"input file '{input}' does not exist");
                }

                FFOptions options = GlobalFFOptions.Current.Clone();

                // we are we passing no CancellationToken to FFProbe when we are given on in the method args?
                // for reasons i don't understand, if the cancellation is called while the probing is going on,
                //      OR (and this is the weird part) after this method completes,
                // then an exception: "No process is associated with this object" is thrown, which we cannot catch
                cancel.ThrowIfCancellationRequested();
                IMediaAnalysis analysis = await FFProbe.AnalyseAsync(input, null, CancellationToken.None);
                cancel.ThrowIfCancellationRequested();
                TimeSpan videoDuration = analysis.Duration;
                _Logger.LogDebug($"input file length: {videoDuration.TotalSeconds} seconds [input={input}]");

                cancel.ThrowIfCancellationRequested();
                await FFMpegArguments.FromFileInput(input)
                    .OutputToFile(output, false, (options) => {
                        options
                            .WithVideoCodec(VideoCodec.LibX264)
                            .WithAudioCodec(AudioCodec.Aac)
                            .ForceFormat("mp4")
                            .WithFastStart(); // https://sanjeev-pandey.medium.com/understanding-the-mpeg-4-moov-atom-pseudo-streaming-in-mp4-93935e1b9e9a
                    })
                    .NotifyOnProgress((double arg0) => {
                        progressCallback((decimal) arg0);
                    }, videoDuration)
                    .NotifyOnOutput((string output) => {
                        _Logger.LogDebug($"ffmpeg output> {output}");
                    })
                    .NotifyOnError((string err) => {
                        // don't print out progress updates
                        // frame= 7092 fps=203 q=31.0 size=   69376kB time=00:01:58.40 bitrate=4799.8kbits/s speed=3.39x
                        if (err.StartsWith("frame=")) {
                            return;
                        }
                        _Logger.LogWarning($"ffmpeg error> {err}");
                    })
                    .CancellableThrough(cancel)
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

                return true;
            }

        }


    }

}
