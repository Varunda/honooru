using FFMpegCore;
using FFMpegCore.Enums;
using honooru.Models.App.MediaUploadStep;
using honooru.Models.Config;
using honooru.Models.Queues;
using honooru.Services.Queues;
using honooru.Services.UploadStepHandler;
using honooru.Services.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted.QueueProcessor {

    public class UploadStepsQueueProcessor : BaseQueueProcessor<UploadSteps> {

        private readonly UploadStepsProcessor _UploadStepsHandler;

        public UploadStepsQueueProcessor(ILoggerFactory factory,
                BaseQueue<UploadSteps> queue, ServiceHealthMonitor serviceHealthMonitor,
                UploadStepsProcessor uploadStepsHandler)
            : base("upload_steps", factory, queue, serviceHealthMonitor) {

            _UploadStepsHandler = uploadStepsHandler;
        }

        protected override async Task<bool> _ProcessQueueEntry(UploadSteps entry, CancellationToken cancel) {
            _Logger.LogInformation($"starting to process {entry.Asset.Guid}");

            try {
                CancellationTokenSource cts = new(TimeSpan.FromMinutes(10));
                await _UploadStepsHandler.Run(entry, cts.Token);
                _Logger.LogInformation($"successfully processed {entry.Asset.Guid}");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to process {entry.Asset.Guid}");
            }

            return true;
        }

    }
}
