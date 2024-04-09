using honooru.Code.Constants;
using System;
using System.Text.Json.Serialization;

namespace honooru.Models.Db {

    public class Post {

        /// <summary>
        ///     unique ID of the post
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     ID of the <see cref="AppAccount"/> that posted this media
        /// </summary>
        public ulong PosterUserID { get; set; }

        /// <summary>
        ///     when this post was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        public PostStatus Status { get; set; }

        /// <summary>
        ///     an optional title
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        ///     an optional description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        ///     the ID of the last <see cref="AppAccount"/> to edit this post
        /// </summary>
        public ulong LastEditorUserID { get; set; }

        /// <summary>
        ///     when this post was last edited
        /// </summary>
        public DateTime? LastEdited { get; set; }

        /// <summary>
        ///     MD5 hash of the file
        /// </summary>
        [JsonPropertyName("md5")] // without this, it's mD5
        public string MD5 { get; set; } = "";

        /// <summary>
        ///     what this post is rated as
        /// </summary>
        public PostRating Rating { get; set; } = PostRating.GENERAL;

        /// <summary>
        ///     original name of the file uploaded
        /// </summary>
        public string FileName { get; set; } = "";

        /// <summary>
        ///     where this post comes from
        /// </summary>
        public string Source { get; set; } = "";

        /// <summary>
        ///     file extension, WITH a leading .
        /// </summary>
        public string FileExtension { get; set; } = "";

        /// <summary>
        ///     hash from the IQDB service
        /// </summary>
        public string IqdbHash { get; set; } = "";

        /// <summary>
        ///     how many bytes. not sure why c# has file size signed
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        ///     how many seconds long this piece of media is. is set to 0 for images
        /// </summary>
        public long DurationSeconds { get; set; }

        /// <summary>
        ///     height of the post
        /// </summary>
        public long Height { get; set; }

        /// <summary>
        ///     width of the post
        /// </summary>
        public long Width { get; set; }

    }

    public enum PostStatus {

        OK = 1,

        DELETED = 2

    }

}
