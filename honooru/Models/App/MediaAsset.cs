using System;
using System.Text.Json.Serialization;

namespace honooru.Models.App {

    public class MediaAsset {

        /// <summary>
        ///     temp guid the file is saved under
        /// </summary>
        public Guid Guid { get; set; } = Guid.Empty;

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

    }

    public enum MediaAssetStatus {

        DEFAULT = 0,

        UPLOADING = 1,

        PROCESSING = 2,

        DONE = 3

    }

}
