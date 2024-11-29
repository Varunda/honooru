using honooru_common.Models;
using honooru_common.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace snail.Services {

    public class YoutubeExtractor {

        private readonly ILogger<YoutubeExtractor> _Logger;
        private readonly PathEnvironmentService _PathUtil;

        public YoutubeExtractor(ILogger<YoutubeExtractor> logger,
            PathEnvironmentService pathUtil) {

            _Logger = logger;
            _PathUtil = pathUtil;
        }

        public async Task Download(DistributedJob job) {
            Stopwatch timer = Stopwatch.StartNew();
            string url = job.Values["url"];

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
            //ytdl.OutputFolder = path;
            ytdl.OutputFolder = $"./downloads/{job.ID}/";

            _Logger.LogDebug($"yt-dlp instanced created [timer={timer.ElapsedMilliseconds}ms] [url=url]");
            timer.Restart();

            OptionSet opts = new();
            opts.FfmpegLocation = Path.GetDirectoryName(ffmpegPath);
            _Logger.LogDebug($"yt-dlp ffmpeg location set [FfmpegLocation={opts.FfmpegLocation}] [based on={ffmpegPath}]");

            float previousProgress = 0f;
            RunResult<string> data = await ytdl.RunVideoDownload(url,
                progress: new Progress<DownloadProgress>(p => {
                    // for some reason, the downloader outputs 0 in-between steps, such as:
                    //      0.1, 0.2, 0, 0.3
                    // so, ignore those that are smaller than the last progress update
                    if (previousProgress <= p.Progress) {
                        //progress((decimal)(p.Progress * 100f));
                        previousProgress = p.Progress;
                    }
                }),
                output: new Progress<string>(o => {
                    if (string.IsNullOrWhiteSpace(o) || o.StartsWith("[download]")) {
                        return;
                    }

                    _Logger.LogDebug($"ytdlp output([url={url}])> {o} ");
                }),

                overrideOptions: opts
            );

            if (data.ErrorOutput.Length > 0) {
                _Logger.LogError($"youtube extractor had error output:");

                string s = string.Join("\n", data.ErrorOutput);
                _Logger.LogError(s);
            }
        }


    }
}
