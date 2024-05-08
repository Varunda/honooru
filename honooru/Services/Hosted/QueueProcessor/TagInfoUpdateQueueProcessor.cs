using honooru.Models.App;
using honooru.Models.Db;
using honooru.Models.Queues;
using honooru.Services.Db;
using honooru.Services.Queues;
using honooru.Services.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted.QueueProcessor {

    public class TagInfoUpdateQueueProcessor : BaseQueueProcessor<TagInfoUpdateQueueEntry> {

        private readonly TagInfoRepository _TagInfoRepository;
        private readonly PostTagDb _PostTagDb;

        public TagInfoUpdateQueueProcessor(ILoggerFactory factory,
                BaseQueue<TagInfoUpdateQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
                TagInfoRepository tagInfoRepository, PostTagDb postTagDb)
            : base("tag_info_update", factory, queue, serviceHealthMonitor) {

            _TagInfoRepository = tagInfoRepository;
            _PostTagDb = postTagDb;
        }

        protected async override Task<bool> _ProcessQueueEntry(TagInfoUpdateQueueEntry entry, CancellationToken cancel) {
            _Logger.LogInformation($"updating {nameof(TagInfo)} [tagID={entry.TagID}]");

            List<PostTag> postTags = await _PostTagDb.GetByTagID(entry.TagID);

            TagInfo? info = await _TagInfoRepository.GetByID(entry.TagID);

            if (info == null) {
                info = new TagInfo();
                info.ID = entry.TagID;
                info.Description = null;
                info.Uses = 0;
            }

            info.Uses = (ulong) postTags.Count;

            await _TagInfoRepository.Upsert(info);
            _Logger.LogInformation($"updated {nameof(TagInfo)} [tagID={entry.TagID}] [uses={info.Uses}]");

            return true;
        }

    }
}
