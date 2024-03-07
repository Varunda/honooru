using System;

namespace honooru.Models.App {

    public class MediaAsset {

        /// <summary>
        ///     unique ID
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     md5 hash of the media asset
        /// </summary>
        public string MD5 { get; set; } = "";

        /// <summary>
        ///     original file name
        /// </summary>
        public string FileName { get; set; } = "";

        /// <summary>
        ///     where this file is
        /// </summary>
        public string FileLocation { get; set; } = "";

        /// <summary>
        ///     when this asset was uploaded
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     how many bytes long this fill is. not sure if a negative value is possible.
        ///     using a <see cref="long"/> gives us files over 4GB (which is what an int would limit us to)
        /// </summary>
        public long FileSizeBytes { get; set; }

    }
}
