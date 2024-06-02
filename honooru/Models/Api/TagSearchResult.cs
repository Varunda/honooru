using honooru.Models.App;

namespace honooru.Models.Api {

    public class TagSearchResult {

        public TagSearchResult(Tag t) {
            this.Tag = t;
        }

        public TagSearchResult(Tag t, TagAlias alias) {
            this.Tag = t;
            this.Alias = alias;
        }

        public Tag Tag { get; set; }

        public TagAlias? Alias { get; set; } = null;

    }
}
