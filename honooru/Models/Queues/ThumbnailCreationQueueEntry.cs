namespace honooru.Models.Queues {

    public class ThumbnailCreationQueueEntry {

        /// <summary>
        ///     md5 hash 
        /// </summary>
        public string MD5 { get; set; } = "";

        /// <summary>
        ///     file extension without a leading .
        /// </summary>
        public string FileExtension { get; set; } = "";

        /// <summary>
        ///     will the thumbnail be deleted and recreated if needed?
        /// </summary>
        public bool RecreateIfNeeded { get; set; } = false;

    }
}
