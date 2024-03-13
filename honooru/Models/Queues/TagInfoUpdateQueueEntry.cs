using honooru.Models.App;

namespace honooru.Models.Queues {

    /// <summary>
    ///     entry to perform an update on a <see cref="TagInfo"/>
    /// </summary>
    public class TagInfoUpdateQueueEntry {

        public ulong TagID { get; set; } = 0;

    }
}
