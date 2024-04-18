using honooru.Models.App;
using honooru.Models.App.MediaUploadStep;
using honooru.Models.Config;
using honooru.Services.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace honooru.Services.UrlMediaExtrator {

    public class YoutubeExtractor : IUrlMediaExtractor {

        public string Name => "youtube";

        public bool NeedsQueue => true;

        private readonly ILogger<YoutubeExtractor> _Logger;

        private readonly PathEnvironmentService _PathUtil;

        public YoutubeExtractor(ILogger<YoutubeExtractor> logger,
            PathEnvironmentService pathUtil) {

            _Logger = logger;
            _PathUtil = pathUtil;
        }

        public bool CanHandle(Uri url) {
            return url.Host == "www.youtube.com"
                || url.Host == "youtu.be"
                || url.Host == "youtube.com"
                || url.Host == "www.twitch.tv";
        }

        public async Task Handle(Uri url, StorageOptions options, MediaAsset asset, Action<decimal> progress) {
            string path = Path.Combine(options.RootDirectory, "upload", asset.Guid.ToString());
            _Logger.LogInformation($"downloading url using yt-dlp extractor [url={url.OriginalString}] [path={path}]");

            Stopwatch timer = Stopwatch.StartNew();

            string? ytdlpPath = _PathUtil.FindExecutable("yt-dlp");
            if (ytdlpPath == null) {
                _Logger.LogError($"failed to find yt-dlp executable in PATH! this will cause issues");
            }

            string? ffmpegPath = _PathUtil.FindExecutable("ffmpeg");
            if (ffmpegPath == null) {
                _Logger.LogError($"failed to find ffmpeg executable in PATH! this will cause issues");
            }

            _Logger.LogDebug($"created ytdlp instance [ytdlpPath={ytdlpPath}] [ffmpegPath={ffmpegPath}]");
            YoutubeDL ytdl = new();
            ytdl.YoutubeDLPath = ytdlpPath;
            ytdl.FFmpegPath = ffmpegPath;
            ytdl.OutputFolder = path;

            _Logger.LogDebug($"yt-dlp instanced created [timer={timer.ElapsedMilliseconds}ms] [assetID={asset.Guid}]");
            timer.Restart();

            float previousProgress = 0f;
            RunResult<string> data = await ytdl.RunVideoDownload(url.ToString(),
                progress: new Progress<DownloadProgress>(p => {
                    // for some reason, the downloader outputs 0 in-between steps, such as:
                    //      0.1, 0.2, 0, 0.3
                    // so, ignore those that are smaller than the last progress update
                    if (previousProgress <= p.Progress) {
                        progress((decimal) (p.Progress * 100f));
                        previousProgress = p.Progress;
                    }
                }),
                output: new Progress<string>(o => {
                    //_Logger.LogDebug($"ytdlp output> {o}");
                })
            );

            if (data.ErrorOutput.Length > 0) {
                _Logger.LogError($"youtube extractor had error output:");

                string s = string.Join("\n", data.ErrorOutput);
                _Logger.LogError(s);
            }

            _Logger.LogDebug($"yt-dlp download complete [timer={timer.ElapsedMilliseconds}ms] [url={url}] [assetID={asset.Guid}]");
            timer.Restart();

            if (Path.Exists(path) == false) {
                _Logger.LogError($"failed to find output path, expect errors! [path={path}]");
            }

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

            _Logger.LogDebug($"yt-dlp download moved to upload folder [timer={timer.ElapsedMilliseconds}ms] [url={url}] [assetID={asset.Guid}]");
            timer.Restart();

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
            } else {
                _Logger.LogError($"failed to get video metadata: {string.Join("\n", videoData.ErrorOutput)}");
            }

            _Logger.LogDebug($"yt-dlp data download complete [timer={timer.ElapsedMilliseconds}ms] [url={url}] [assetID={asset.Guid}]");
            timer.Restart();

            FileInfo info = new FileInfo(target);
            _Logger.LogDebug($"yt-dlp download {nameof(FileInfo)} loaded [timer={timer.ElapsedMilliseconds}ms] [assetID={asset.Guid}] [target={target}]");
            asset.FileSizeBytes = info.Length;
        }

    }
}
