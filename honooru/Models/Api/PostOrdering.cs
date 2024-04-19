using honooru.Models.Db;

namespace honooru.Models.Api {

    public class PostOrdering {

        public string Query { get; set; } = "";

        public ulong PostID { get; set; }

        public Post? Previous { get; set; } = null;

        public Post? Next { get; set; } = null;

    }
}
