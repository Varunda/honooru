using FFMpegCore;
using FFMpegCore.Enums;
using honooru.Models.Config;
using honooru.Models.Queues;
using honooru.Services.Queues;
using honooru.Services.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted.QueueProcessor {

    public class MediaAssetReencodeQueueProcessor : BaseQueueProcessor<MediaAssetReencodeQueueEntry> {

        private readonly IOptions<StorageOptions> _StorageOptions;
        private readonly FileExtensionService _FileExtensionHelper;

        public MediaAssetReencodeQueueProcessor(ILoggerFactory factory,
                BaseQueue<MediaAssetReencodeQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
                IOptions<StorageOptions> storageOptions, FileExtensionService fileExtensionHelper)
            : base("media_asset_reencode", factory, queue, serviceHealthMonitor) {

            _StorageOptions = storageOptions;
            _FileExtensionHelper = fileExtensionHelper;
        }

        protected override async Task<bool> _ProcessQueueEntry(MediaAssetReencodeQueueEntry entry, CancellationToken cancel) {
            string path = Path.Combine(_StorageOptions.Value.RootDirectory, "original", entry.MD5 + entry.FileExtension);
            _Logger.LogInformation($"starting reencode [entry.MD5={entry.MD5}] [entry.FileExtension={entry.FileExtension}] [path={path}]");

            if (File.Exists(path) == false) {
                _Logger.LogError($"cannot reencode: source file is missing [path={path}]");
                return false;
            }

            string? fileType = _FileExtensionHelper.GetFileType(entry.FileExtension);
            if (fileType != "video") {
                _Logger.LogError($"cannot reencode: expected to get fileType of 'video' [actual={fileType}] [extension={entry.FileExtension}]");
                return false;
            }

            string output = Path.Combine(_StorageOptions.Value.RootDirectory, "original", entry.MD5 + ".mp4");

            Stopwatch timer = Stopwatch.StartNew();
            await FFMpegArguments.FromFileInput(path)
                .OutputToFile(output, false, (options) => {
                    options
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithFastStart();
                }).ProcessAsynchronously();

            _Logger.LogInformation($"processing complete [elapsedMs={timer.ElapsedMilliseconds}ms]");

            if (File.Exists(output) == false) {
                _Logger.LogError($"unexpected result: missing output file? [output={output}] [path={path}]");
            }

            return true;
        }

    }
}
