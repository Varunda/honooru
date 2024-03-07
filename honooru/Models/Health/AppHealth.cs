using System;
using System.Collections.Generic;
using honooru.Models.Api;

namespace honooru.Models.Health {

    /// <summary>
    ///     Information about the health of the running app
    /// </summary>
    public class AppHealth {

        /// <summary>
        ///     Information about the hosted queues in the running app
        /// </summary>
        public List<ServiceQueueCount> Queues { get; set; } = new List<ServiceQueueCount>();

        /// <summary>
        ///     When this data was created
        /// </summary>
        public DateTime Timestamp { get; set; }

    }
}
