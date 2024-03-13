using honooru.Models.Queues;
using Microsoft.Extensions.Logging;

namespace honooru.Services.Queues {

    public class TagInfoUpdateQueue : BasePendingQueue<TagInfoUpdateQueueEntry, ulong> {

        public TagInfoUpdateQueue(ILoggerFactory factory) : base(factory) { }

        internal override ulong GetEntryID(TagInfoUpdateQueueEntry entry) => entry.TagID;

    }
}
