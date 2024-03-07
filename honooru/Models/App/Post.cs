using honooru.Code.Constants;
using System;

namespace honooru.Models.Db {

    public class Post {

        /// <summary>
        ///     unique ID of the post
        /// </summary>
        public ulong ID { get; set; }

        public ulong PosterUserID { get; set; }

        public DateTime Timestamp { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public ulong LastEditorUserID { get; set; }

        public DateTime? LastEdited { get; set; }

        public string MD5 { get; set; } = "";

        public PostRating Rating { get; set; } = PostRating.GENERAL;

        public string FileName { get; set; } = "";

        public string Source { get; set; } = "";

        public string FileLocation { get; set; } = "";

        public long FileSizeBytes { get; set; }

    }
}
