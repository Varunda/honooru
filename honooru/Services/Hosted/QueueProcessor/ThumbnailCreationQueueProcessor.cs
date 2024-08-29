using FFMpegCore;
using honooru.Models.Config;
using honooru.Models.Queues;
using honooru.Services.Queues;
using honooru.Services.Util;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted.QueueProcessor {

    public class ThumbnailCreationQueueProcessor : BaseQueueProcessor<ThumbnailCreationQueueEntry> {

        private readonly IOptions<StorageOptions> _StorageOptions;
        private readonly FileExtensionService _FileExtensionHelper;

        public ThumbnailCreationQueueProcessor(ILoggerFactory factory,
                BaseQueue<ThumbnailCreationQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
                IOptions<StorageOptions> storageOptions, FileExtensionService fileExtensionHelper)
            : base("thumbnail_creation", factory, queue, serviceHealthMonitor) {

            _StorageOptions = storageOptions;
            _FileExtensionHelper = fileExtensionHelper;
        }

        protected override async Task<bool> _ProcessQueueEntry(ThumbnailCreationQueueEntry entry, CancellationToken cancel) {
            string path = Path.Combine(_StorageOptions.Value.RootDirectory, "original", entry.MD5[..2], entry.MD5 + "." + entry.FileExtension);
            _Logger.LogInformation($"processing thumbnail creation [entry.MD5={entry.MD5}] [entry.FileExtension={entry.FileExtension}] [path={path}]");

            if (File.Exists(path) == false) {
                _Logger.LogError($"failed to find file to create a thumbnail of [entry.MD5={entry.MD5}] [entry.FileExtension={entry.FileExtension}] [path={path}]");
                return false;
            }

            string md5 = Path.GetFileNameWithoutExtension(path);
            string thumbnailPath = Path.Combine(_StorageOptions.Value.RootDirectory, "180x180", md5[..2], md5 + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath) ?? "");

            // recreate the thumbnail only if needed
            _Logger.LogDebug($"checking if thumbnail exists [md5={md5}] [recreateIfNeeded={entry.RecreateIfNeeded}] [thumbnailPath={thumbnailPath}]");
            if (File.Exists(thumbnailPath) == true) {
                if (entry.RecreateIfNeeded == false) {
                    _Logger.LogWarning($"thumbnail already exists! skipping [md5={md5}] [thumbnailPath={thumbnailPath}]");
                    return false;
                } else {
                    _Logger.LogInformation($"recreating thumbnail [md5={md5}] [thumbnailPath={thumbnailPath}]");
                    File.Delete(thumbnailPath);
                }
            }

            string fileExt = Path.GetExtension(path)[1..].ToLower(); // remove the leading .
            _Logger.LogInformation($"checking extension for how to handle it [fileExt={fileExt}]");

            string? fileType = _FileExtensionHelper.GetFileType(fileExt);
            if (fileType == null) {
                _Logger.LogError($"failed to create thumbnail: fileType is null! [fileExt={fileExt}] [path={path}]");
                return false;
            }

            Stopwatch timer = Stopwatch.StartNew();
            if (fileType == "image") {
                await _ProcessImage(path, thumbnailPath, cancel);
            } else if (fileType == "pdf") {
                await _ProcessPdf(path, thumbnailPath, cancel);
            } else if (fileType == "video") {
                await _ProcessVideo(path, thumbnailPath, cancel);
            } else {
                throw new System.Exception($"unhandled file type: '{fileType}'");
            }

            _Logger.LogInformation($"successfully created thumbnail [time={timer.ElapsedMilliseconds}ms] [path={path}] [thumbnailPath={thumbnailPath}]");

            return true;
        }

        private async Task _ProcessImage(string input, string output, CancellationToken cancel) {
            using MagickImage mImage = new(input);
            mImage.Strip();

            if (mImage.Width > mImage.Height) {
                mImage.Resize(180, 0);
            } else {
                mImage.Resize(0, 180);
            }
            mImage.Extent(180, 180, Gravity.Center, MagickColor.FromRgba(0, 0, 0, 0));

            await mImage.WriteAsync(output, cancel);
        }

        private async Task _ProcessPdf(string input, string output, CancellationToken cancel) {
            using MagickImageCollection coll = new();

            // get only the first page
            await coll.ReadAsync(input, new MagickReadSettings() {
                FrameIndex = 0,
                FrameCount = 1,
            }, cancel);

            if (coll.Count == 0) {
                _Logger.LogWarning($"failed to read a page from the PDF file [input={input}]");
            }

            using IMagickImage<ushort> firstPage = coll.ElementAt(0);
            firstPage.Strip();
            firstPage.BackgroundColor = MagickColor.FromRgb(0xff, 0xff, 0xff);
            firstPage.Alpha(AlphaOption.Remove);
            firstPage.Alpha(AlphaOption.Off);

            if (firstPage.Width > firstPage.Height) {
                firstPage.Resize(180, 0);
            } else {
                firstPage.Resize(0, 180);
            }
            firstPage.Extent(180, 180, Gravity.Center, MagickColor.FromRgba(0, 0, 0, 0));

            await firstPage.WriteAsync(output, cancel);
        }

        private async Task _ProcessVideo(string input, string output, CancellationToken cancel) {
            IMediaAnalysis dets = FFProbe.Analyse(input);

            int height = dets.PrimaryVideoStream?.Height ?? 0;
            int width = dets.PrimaryVideoStream?.Width ?? 0;

            await FFMpeg.SnapshotAsync(input, output, new System.Drawing.Size() {
                Width = width > height ? 180 : 0,
                Height = width > height ? 0 : 180
            });

            using MagickImage mImage = new(output);
            mImage.Strip();
            mImage.Extent(180, 180, Gravity.Center, MagickColor.FromRgba(0, 0, 0, 0));

            await mImage.WriteAsync(output, cancel);
        }

    }
}
