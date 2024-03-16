using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using honooru.Services.Repositories;
using honooru.Services.UploadStepHandler;
using honooru.Models.Api;
using Microsoft.AspNetCore.Authorization;

namespace honooru.Code.Hubs.Implementations {

    [AllowAnonymous]
    public class MediaAssetUploadHub : Hub<IMediaAssetUploadHub> {

        private readonly ILogger<MediaAssetUploadHub> _Logger;
        private readonly UploadStepProgressRepository _UploadProgressRepository;

        public MediaAssetUploadHub(ILogger<MediaAssetUploadHub> logger, 
            UploadStepProgressRepository uploadProgressRepository) {

            _Logger = logger;

            _UploadProgressRepository = uploadProgressRepository;
        }

        public override async Task OnConnectedAsync() {
            _Logger.LogInformation($"New connection: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public async Task SubscribeToMediaAsset(string input) {
            Guid guid = Guid.Parse(input);

            string connID = Context.ConnectionId;
            string groupName = $"MediaAsset.Upload.{guid}";

            _Logger.LogInformation($"new subscriber to media asset [connID={connID}] [Guid={guid}] [groupName={groupName}]");

            await Groups.AddToGroupAsync(connID, groupName);

            UploadStepEntry? entry = _UploadProgressRepository.GetByMediaAssetID(guid);
            if (entry != null) {
                await Clients.Caller.UpdateProgress(entry);
            }
        }

    }
}
