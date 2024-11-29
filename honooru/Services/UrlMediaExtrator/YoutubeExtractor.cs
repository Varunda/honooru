using honooru.Models.App;
using honooru.Models.App.MediaUploadStep;
using honooru.Models.Config;
using honooru.Services.Db;
using honooru.Services.Repositories;
using honooru.Services.Util;
using honooru_common.Models;
using honooru_common.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private readonly IOptions<ExtractorOptions> _ExtractorOptions;

        private readonly ExtractorAuthorMappingRepository _AuthorMappingRepository;
        private readonly TagRepository _TagRepository;
        private readonly DistributedJobDb _JobDb;
        private readonly MediaAssetRepository _MediaAssetRepository;

        private readonly PathEnvironmentService _PathUtil;

        public YoutubeExtractor(ILogger<YoutubeExtractor> logger, IOptions<ExtractorOptions> options,
            PathEnvironmentService pathUtil, ExtractorAuthorMappingRepository authorMappingRepository,
            TagRepository tagRepository, DistributedJobDb jobDb,
            MediaAssetRepository mediaAssetRepository) {

            _Logger = logger;
            _ExtractorOptions = options;

            _PathUtil = pathUtil;
            _AuthorMappingRepository = authorMappingRepository;
            _TagRepository = tagRepository;
            _JobDb = jobDb;
            _MediaAssetRepository = mediaAssetRepository;
        }

        public bool CanHandle(Uri url) {
            return IsYoutubeLink(url.Host)
                || url.Host == "www.twitch.tv"
                || url.Host == "clips.twitch.tv"
                || url.Host == "streamable.com";
        }

        private bool IsYoutubeLink(string host) {
            return host == "www.youtube.com"
                || host == "youtu.be"
                || host == "youtube.com";
        }

        public async Task<MediaAsset> Handle(Uri url, StorageOptions options, MediaAsset asset, Action<decimal> progress) {
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

            OptionSet opts = new();
            opts.FfmpegLocation = Path.GetDirectoryName(ffmpegPath);
            _Logger.LogDebug($"yt-dlp ffmpeg location set [FfmpegLocation={opts.FfmpegLocation}] [based on={ffmpegPath}]");

            // if this is a youtube link, and the server is configured to have worker clients download videos, do that
            if (IsYoutubeLink(url.Host) && _ExtractorOptions.Value.DistributeYoutubeJobs == true) {
                DistributedJob job = new();
                job.ID = asset.Guid;
                job.Type = DistributedJobType.YOUTUBE;
                job.Values.Add("url", url.ToString());

                _Logger.LogInformation($"creating distributed job for youtube link [url={url.ToString()}] [ID={asset.Guid}]");

                await _JobDb.Upsert(job);

                // wait for the job to be odne
                DateTime failsafeEnd = DateTime.UtcNow + TimeSpan.FromHours(1);
                while (true) {
                    if (failsafeEnd < DateTime.UtcNow) {
                        _Logger.LogError($"took more than 1 hour to wait for distributed job to finish [ID={asset.Guid}]");
                        break;
                    }

                    DistributedJob? pending = await _JobDb.GetByID(asset.Guid);

                    if (pending == null) {
                        _Logger.LogError($"where did the job go? [ID={asset.Guid}]");
                        break;
                    }

                    if (pending.Done == true) {
                        _Logger.LogInformation($"job done [ID={asset.Guid}]");
                        await _JobDb.Delete(asset.Guid);
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10));
                }

                // the job worker may have changed data about the asset, reload it from DB to make sure it's up to date
                asset = await _MediaAssetRepository.GetByID(asset.Guid)
                    ?? throw new Exception($"somehow the media asset {asset.Guid} is now missing?");

                _Logger.LogInformation($"reloaded media asset [ID={asset.Guid}] [MD5={asset.MD5}]");

                string jobSource = Path.Combine(options.RootDirectory, "work", asset.MD5 + "." + asset.FileExtension);
                string jobTarget = Path.Combine(options.RootDirectory, "upload", asset.Guid.ToString() + "." + asset.FileExtension);

                _Logger.LogDebug($"moving distributed job from work into upload");
                if (File.Exists(jobTarget) == false) {
                    File.Move(jobSource, jobTarget);
                } else {
                    _Logger.LogDebug($"move already complete [input={jobSource}] [output={jobTarget}]");
                }

            } else {
                float previousProgress = 0f;
                RunResult<string> data = await ytdl.RunVideoDownload(url.ToString(),
                    progress: new Progress<DownloadProgress>(p => {
                        // for some reason, the downloader outputs 0 in-between steps, such as:
                        //      0.1, 0.2, 0, 0.3
                        // so, ignore those that are smaller than the last progress update
                        if (previousProgress <= p.Progress) {
                            progress((decimal)(p.Progress * 100f));
                            previousProgress = p.Progress;
                        }
                    }),
                    output: new Progress<string>(o => {
                        if (string.IsNullOrWhiteSpace(o) || o.StartsWith("[download]")) {
                            return;
                        }

                        _Logger.LogDebug($"ytdlp output([assetID={asset.Guid}])> {o} ");
                    }),

                    overrideOptions: opts
                );

                if (data.ErrorOutput.Length > 0) {
                    _Logger.LogError($"youtube extractor had error output:");

                    string s = string.Join("\n", data.ErrorOutput);
                    _Logger.LogError(s);
                }

                // the file wanted is in a folder with the GUID as the name
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
            }

            _Logger.LogDebug($"yt-dlp download complete [timer={timer.ElapsedMilliseconds}ms] [url={url}] [assetID={asset.Guid}]");
            timer.Restart();

            RunResult<YoutubeDLSharp.Metadata.VideoData> videoData = await ytdl.RunVideoDataFetch(url.ToString());
            if (videoData.Success) {
                // removed unwanted data
                videoData.Data.AutomaticCaptions = new Dictionary<string, YoutubeDLSharp.Metadata.SubtitleData[]>();
                videoData.Data.Thumbnails = [];
                videoData.Data.Formats = [];
                _Logger.LogInformation($"metadata found from video: {videoData.Data.ToString().Replace("\n", "")}");

                DateTime? timestamp = videoData.Data.Timestamp ?? videoData.Data.UploadDate;
                if (timestamp != null) {
                    asset.AdditionalTags += $" meta:{timestamp.Value.Year}";
                }

                if (videoData.Data.Extractor.StartsWith("twitch")) { // twitch:vod twitch:clip
                    asset.AdditionalTags += " twitch";
                } else if (videoData.Data.Extractor == "youtube") {
                    asset.AdditionalTags += " youtube";
                } else if (videoData.Data.Extension == "streamable") {
                    asset.AdditionalTags += " streamable";
                }

                // check if the channel is mapped to a tag
                string site = videoData.Data.Extractor ?? "";
                string channel = videoData.Data.Channel ?? "";
                _Logger.LogDebug($"performing author mapping [site={site}] [channel={channel}]");
                ExtractorAuthorMapping? mapping = await _AuthorMappingRepository.GetMapping(site, channel);
                if (mapping != null) {
                    _Logger.LogDebug($"author mapping found [site={site}] [channel={channel}] [mapping.TagID={mapping.TagID}]");

                    Tag? tag = await _TagRepository.GetByID(mapping.TagID);
                    if (tag != null) {
                        _Logger.LogDebug($"adding tag based on author mapping [site={site}] [channel={channel}] [tag.Name={tag.Name}]");
                        asset.AdditionalTags += $" {tag.Name}";
                    } else {
                        _Logger.LogWarning($"missing tag from author mapping [mapping.TagID={mapping.TagID}]");
                    }
                }

                asset.Title = videoData.Data.Title ?? "";
                asset.Description = videoData.Data.Description ?? "";
            } else {
                _Logger.LogError($"failed to get video metadata: {string.Join("\n", videoData.ErrorOutput)}");
            }

            _Logger.LogDebug($"yt-dlp data download complete [timer={timer.ElapsedMilliseconds}ms] [url={url}] [assetID={asset.Guid}]");
            timer.Restart();

            string downloadedFile = Path.Combine(options.RootDirectory, "upload", asset.Guid.ToString() + "." + asset.FileExtension);
            FileInfo info = new FileInfo(downloadedFile);
            _Logger.LogDebug($"yt-dlp download {nameof(FileInfo)} loaded [timer={timer.ElapsedMilliseconds}ms] [assetID={asset.Guid}] [downloadedFile={downloadedFile}]");
            asset.FileSizeBytes = info.Length;

            return asset;
        }

    }
}
