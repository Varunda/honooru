using System;

namespace honooru.Models.Queues {

    /// <summary>
    ///     Queue entries for when a session is ended
    /// </summary>
    public class ExampleQueueEntry {

        /// <summary>
        ///     ID
        /// </summary>
        public ulong ID { get; set; } 

        /// <summary>
        ///     timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

    }
}
