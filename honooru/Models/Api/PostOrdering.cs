using honooru.Models.Db;

namespace honooru.Models.Api {

    public class PostOrdering {

        /// <summary>
        ///     query used to perform the search
        /// </summary>
        public string Query { get; set; } = "";

        /// <summary>
        ///     ID of the post to get the previous and next of
        /// </summary>
        public ulong PostID { get; set; }

        /// <summary>
        ///     the <see cref="Post"/> that is previous in order to the <see cref="Post"/> with <see cref="Post.ID"/>
        ///     of <see cref="PostID"/> when <see cref="Query"/> is performed. is null if the post is the first one
        /// </summary>
        public Post? Previous { get; set; } = null;

        /// <summary>
        ///     the <see cref="Post"/> that is next in order to the <see cref="Post"/> with <see cref="Post.ID"/>
        ///     of <see cref="PostID"/> when <see cref="Query"/> is performed. is null if the post is the last one
        /// </summary>
        public Post? Next { get; set; } = null;

    }
}
