using honooru.Models.Queues;
using Microsoft.Extensions.Logging;

namespace honooru.Services.Queues {

    public class ThumbnailCreationQueue : BaseQueue<ThumbnailCreationQueueEntry> {

        public ThumbnailCreationQueue(ILoggerFactory factory)
            : base(factory) { }

    }
}
