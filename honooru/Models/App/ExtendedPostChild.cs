using honooru.Models.Db;

namespace honooru.Models.App {

    public class ExtendedPostChild {

        public PostChild PostChild { get; set; } = new();

        public Post? Parent { get; set; }

        public Post? Child { get; set; }

    }
}
