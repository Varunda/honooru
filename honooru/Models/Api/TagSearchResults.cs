using System.Collections.Generic;

namespace honooru.Models.Api {

    public class TagSearchResults {

        public string Input { get; set; } = "";

        public List<ExtendedTag> Tags { get; set; } = new();

    }
}
