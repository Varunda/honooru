namespace honooru.Models.Config {

    public class ExtractorOptions {

        /// <summary>
        ///     will downloads from youtube instead be distributed to worker clients (snail)?
        /// </summary>
        public bool DistributeYoutubeJobs { get; set; } = false;

    }
}
