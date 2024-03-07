using System.Collections.Generic;

namespace honooru.Models.Api {

    public class SearchQuery {

        public string Input { get; set; } = "";

        public HashSet<ulong> TagIds { get; set; } = new();

    }

    public class SearchTermTags {

        public List<string> Tags { get; set; } = new();

        public List<string> Exclude { get; set; } = new();

    }

}
