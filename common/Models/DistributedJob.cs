namespace honooru_common.Models {

    public class DistributedJob {

        /// <summary>
        ///     ID that is used to identify a job
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        ///     what type of job this is, i.e. what type of work will a snail worker do
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        ///     is this job done being worked on?
        /// </summary>
        public bool Done { get; set; }

        /// <summary>
        ///     ID of the user that has claimed to work on this
        /// </summary>
        public ulong? ClaimedByUserID { get; set; }

        /// <summary>
        ///     when this job was claimed by the user
        /// </summary>
        public DateTime? ClaimedAt { get; set; }

        /// <summary>
        ///     when the user working on this claim last updated and confirmed they were working on this job
        /// </summary>
        public DateTime? LastProgressUpdate { get; set; }

        /// <summary>
        ///     dictionary of values used to process the job
        /// </summary>
        public Dictionary<string, string> Values { get; set; } = new();

    }

    public static class DistributedJobType {

        public const string UNKNOWN = "unknown";

        public const string YOUTUBE = "youtube";

    }

    public static class DistributedJobStatus {

        public const string UNCLAIMED = "unclaimed";

        public const string CLAIMED = "claimed";

    }
}
