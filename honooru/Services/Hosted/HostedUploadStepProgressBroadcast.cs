using honooru.Code.Hubs;
using honooru.Code.Hubs.Implementations;
using honooru.Models.Api;
using honooru.Services.UploadStepHandler;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted {

    public class HostedUploadStepProgressBroadcast : BackgroundService {

        private readonly ILogger<HostedUploadStepProgressBroadcast> _Logger;
        private readonly IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> _UploadHub;
        private readonly UploadStepProgressRepository _UploadStepRepository;

        public HostedUploadStepProgressBroadcast(ILogger<HostedUploadStepProgressBroadcast> logger,
            IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> uploadHub,
            UploadStepProgressRepository uploadStepRepository) {

            _Logger = logger;

            _UploadHub = uploadHub;
            _UploadStepRepository = uploadStepRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _Logger.LogInformation($"starting");

            while (stoppingToken.IsCancellationRequested == false) {
                try {
                    List<UploadStepEntry> entries = _UploadStepRepository.GetAll();

                    foreach (UploadStepEntry entry in entries) {
                        await _UploadHub.Clients.Group($"MediaAsset.Upload.{entry.MediaAssetID}").UpdateProgress(entry);
                    }
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to broadcast upload progress");
                }

                await Task.Delay(3000, stoppingToken);
            }

            _Logger.LogInformation($"stopping");
        }

    }
}
