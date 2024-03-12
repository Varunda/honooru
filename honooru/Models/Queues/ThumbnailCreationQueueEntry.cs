namespace honooru.Models.Queues {

    public class ThumbnailCreationQueueEntry {

        /// <summary>
        ///     md5 hash plus the file extension
        /// </summary>
        public string FileName { get; set; } = "";

        /// <summary>
        ///     will the thumbnail be deleted and recreated if needed?
        /// </summary>
        public bool RecreateIfNeeded { get; set; } = false;

    }
}
