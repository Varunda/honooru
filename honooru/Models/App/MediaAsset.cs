using System;
using System.Text.Json.Serialization;

namespace honooru.Models.App {

    public class MediaAsset {

        /// <summary>
        ///     temp guid the file is saved under
        /// </summary>
        public Guid Guid { get; set; } = Guid.Empty;
        
        /// <summary>
        ///     if this <see cref="MediaAsset"/> uploaded exists as a post already, this is the ID of that post
        /// </summary>
        public ulong? PostID { get; set; }

        /// <summary>
        ///     md5 hash of the media asset
        /// </summary>
        [JsonPropertyName("md5")]
        public string MD5 { get; set; } = "";

        /// <summary>
        ///     status of the media asset
        /// </summary>
        public MediaAssetStatus Status { get; set; } = MediaAssetStatus.DEFAULT;

        /// <summary>
        ///     original file name
        /// </summary>
        public string FileName { get; set; } = "";

        /// <summary>
        ///     where this file is. does not include a leading period
        /// </summary>
        public string FileExtension { get; set; } = "";

        /// <summary>
        ///     when this asset was uploaded
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        ///     how many bytes long this fill is. not sure if a negative value is possible.
        ///     using a <see cref="long"/> gives us files over 4GB (which is what an int would limit us to)
        /// </summary>
        public long FileSizeBytes { get; set; } = 0;

        /// <summary>
        ///     where this media asset came from
        /// </summary>
        public string Source { get; set; } = "";

        /// <summary>
        ///     additional tags that are added during parsing
        /// </summary>
        public string AdditionalTags { get; set; } = "";

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

    }

    public enum MediaAssetStatus {

        /// <summary>
        ///     this media asset is in the default state and has not been processed yet
        /// </summary>
        DEFAULT = 0,

        /// <summary>
        ///     this media asset has been queued for processing
        /// </summary>
        QUEUED = 1,

        /// <summary>
        ///     this media asset is currently being processed
        /// </summary>
        PROCESSING = 2,

        /// <summary>
        ///     this media asset is done and ready for tagging to become a post
        /// </summary>
        DONE = 3,

        /// <summary>
        ///     this media asset is being pulled from a URL
        /// </summary>
        EXTRACTING = 4,

    }

}
