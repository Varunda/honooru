using honooru.Models.Queues;
using Microsoft.Extensions.Logging;

namespace honooru.Services.Queues {

    public class MediaAssetReencodeQueue : BaseQueue<MediaAssetReencodeQueueEntry> {

        public MediaAssetReencodeQueue(ILoggerFactory factory) : base(factory) { }

    }

}
