using honooru.Models.App;
using honooru.Models.App.MediaUploadStep;
using honooru.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YoutubeDLSharp;

namespace honooru.Services.UrlMediaExtrator {

    public class YoutubeExtractor : IUrlMediaExtractor {

        public string Name => "youtube";

        public bool NeedsQueue => true;

        private readonly ILogger<YoutubeExtractor> _Logger;

        public YoutubeExtractor(ILogger<YoutubeExtractor> logger) {
            _Logger = logger;
        }

        public bool CanHandle(Uri url) {
            return url.Host == "www.youtube.com"
                || url.Host == "youtu.be"
                || url.Host == "www.twitch.tv";
        }

        public async Task Handle(Uri url, StorageOptions options, MediaAsset asset, Action<decimal> progress) {
            string path = Path.Combine(options.RootDirectory, "upload", asset.Guid.ToString());
            _Logger.LogInformation($"downloading url using yt-dlp extractor [url={url.OriginalString}] [path={path}]");

            YoutubeDL ytdl = new();
            ytdl.YoutubeDLPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "./ffmpeg/yt-dlp.exe" : "./ffmpeg/yt-dlp";
            ytdl.FFmpegPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "./ffmpeg/ffmpeg.exe" : "./ffmpeg/ffmpeg";
            ytdl.OutputFolder = path;

            float previousProgress = 0f;
            RunResult<string> data = await ytdl.RunVideoDownload(url.ToString(), progress: new Progress<DownloadProgress>(p => {
                // for some reason, the downloader outputs 0 in-between steps, such as:
                //      0.1, 0.2, 0, 0.3
                // so, ignore those that are smaller than the last progress update
                if (previousProgress <= p.Progress) {
                    progress((decimal) (p.Progress * 100f));
                    previousProgress = p.Progress;
                }
            }));

            string[] files = Directory.GetFiles(path);
            _Logger.LogDebug($"looking for files after download [path={path}] [files={string.Join(", ", files)}]");

            if (files.Length != 1) {
                throw new Exception($"expected to only see 1 file after download, found {files.Length} instead (in {path})");
            }

            string file = files[0];
            string? extension = Path.GetExtension(file);
            if (string.IsNullOrWhiteSpace(extension)) {
                throw new Exception($"expected extension to be not empty");
            }

            extension = extension[1..]; // remove the leading '.'
            asset.FileExtension = extension;
            asset.FileName = Path.GetFileName(file);

            _Logger.LogDebug($"moving file to upload dir [file={file}] [extension={extension}]");
            string source = file;
            string target = Path.Combine(options.RootDirectory, "upload", asset.Guid.ToString() + "." + extension);

            _Logger.LogDebug($"moving file to upload dir [file={file}] [extension={extension}] [source={source}] [target={target}]");
            File.Move(source, target);

            _Logger.LogDebug($"deleting old path [path={path}]");
            Directory.Delete(path);

            RunResult<YoutubeDLSharp.Metadata.VideoData> videoData = await ytdl.RunVideoDataFetch(url.ToString());
            if (videoData.Success) {
                videoData.Data.AutomaticCaptions = new Dictionary<string, YoutubeDLSharp.Metadata.SubtitleData[]>();
                videoData.Data.Thumbnails = [];
                videoData.Data.Formats = [];
                _Logger.LogInformation($"metadata found from video: {videoData.Data}");

                DateTime? timestamp = videoData.Data.Timestamp ?? videoData.Data.UploadDate;
                if (timestamp != null) {
                    asset.AdditionalTags += $" meta:{timestamp.Value.Year}";
                }

                if (videoData.Data.Extractor.StartsWith("twitch")) {
                    asset.AdditionalTags += " twitch";
                } else if (videoData.Data.Extractor == "youtube") {
                    asset.AdditionalTags += " youtube";
                }

                asset.Title = videoData.Data.Title ?? "";
                asset.Description = videoData.Data.Description ?? "";
            }

            FileInfo info = new FileInfo(target);
            asset.FileSizeBytes = info.Length;
        }

    }
}
