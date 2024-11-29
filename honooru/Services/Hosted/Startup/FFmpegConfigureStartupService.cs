using honooru.Models.Config;
using honooru.Services.Util;
using honooru_common.Services;
using Instances;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted.Startup {

    public class FFmpegConfigureStartupService : IHostedService {

        private readonly ILogger<FFmpegConfigureStartupService> _Logger;

        private readonly PathEnvironmentService _PathUtil;

        public FFmpegConfigureStartupService(ILogger<FFmpegConfigureStartupService> logger,
            PathEnvironmentService pathUtil) {

            _Logger = logger;
            _PathUtil = pathUtil;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {

            string? ffmpegPath = _PathUtil.FindExecutable("ffmpeg");
            string? ffprobePath = _PathUtil.FindExecutable("ffprobe");

            _Logger.LogDebug($"ffmpeg executable search done [ffmpegPath={ffmpegPath}] [ffprobePath={ffprobePath}]");

            if (ffmpegPath == null || ffprobePath == null) {
                _Logger.LogError($"ffmpeg or ffprobe executable not found!");
                return;
            }

            ProcessArguments procArgs = new(ffmpegPath, "-version");
            IProcessResult exit = await procArgs.StartAndWaitForExitAsync(cancellationToken);
            _Logger.LogInformation($"ffmpeg --version output: {string.Join("\n", exit.OutputData)}");

            IReadOnlyList<FFMpegCore.Enums.Codec> codecs = FFMpegCore.FFMpeg.GetCodecs();
            _Logger.LogInformation($"codecs installed: {string.Join(", ", codecs.Select(iter => iter.Name))}");

            return;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
