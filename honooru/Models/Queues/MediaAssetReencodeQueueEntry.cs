using honooru.Models.App;

namespace honooru.Models.Queues {

    /// <summary>
    ///     a queue entry for reencoding a <see cref="MediaAsset"/> to a format that is more widely usable
    /// </summary>
    public class MediaAssetReencodeQueueEntry {

        public string MD5 { get; set; } = "";

        public string FileExtension { get; set; } = "";

    }
}
