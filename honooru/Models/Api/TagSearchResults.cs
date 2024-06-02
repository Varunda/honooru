using honooru.Models.App;
using System.Collections.Generic;

namespace honooru.Models.Api {

    public class TagSearchResults {

        public string Input { get; set; } = "";

        public List<ExtendedTagSearchResult> Tags { get; set; } = new();

    }

    public class ExtendedTagSearchResult {

        public ExtendedTag Tag { get; set; } = new();

        public TagAlias? Alias { get; set; } = null;

    }

}
