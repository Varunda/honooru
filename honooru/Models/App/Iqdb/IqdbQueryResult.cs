using honooru.Models.Db;

namespace honooru.Models.App.Iqdb {

    public class IqdbQueryResult {

        /// <summary>
        ///     this post ID is the MD5 used to store the data in IQDB
        /// </summary>
        public string PostID { get; set; } = "";

        /// <summary>
        ///     a lotta time this is the same as <see cref="PostID"/>, but for videos, <see cref="PostID"/> has -frame-#
        ///     appended to <see cref="PostID"/>, so this field only has the MD5 of the resources
        /// </summary>
        public string MD5 { get; set; } = "";

        /// <summary>
        ///     score from -100 to 100 that indicates how closely this IQDB entry matches the input hash
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        ///     hash of the IQDB entry
        /// </summary>
        public string Hash { get; set; } = "";

        public Post? Post { get; set; }

        public MediaAsset? MediaAsset { get; set; }

    }
}
