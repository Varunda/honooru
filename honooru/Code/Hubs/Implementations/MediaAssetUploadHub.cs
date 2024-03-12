using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using honooru.Services.Repositories;

namespace honooru.Code.Hubs.Implementations {

    public class MediaAssetUploadHub : Hub<IMediaAssetUploadHub> {

        private readonly ILogger<MediaAssetUploadHub> _Logger;

        public MediaAssetUploadHub(ILogger<MediaAssetUploadHub> logger) {
            _Logger = logger;
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
        }

    }
}
