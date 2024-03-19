using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace honooru.Code.Hubs.Implementations {

    [AllowAnonymous]
    public class MediaAssetExtractorHub : Hub<IMediaAssetExtractorHub> {

        private readonly ILogger<MediaAssetExtractorHub> _Logger;

        public MediaAssetExtractorHub(ILogger<MediaAssetExtractorHub> logger) {
            _Logger = logger;
        }

    }
}
