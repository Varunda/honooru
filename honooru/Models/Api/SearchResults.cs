using honooru.Models.Db;
using System.Collections.Generic;

namespace honooru.Models.Api {

    public class SearchResults {

        public SearchResults(SearchQuery query) {
            Query = query;
            ParsedAst = query.QueryAst.Print();
        }

        public SearchQuery Query { get; set; }

        public List<string> Timings { get; set; } = new();

        public string ParsedAst { get; set; } = "";

        public List<Post> Results { get; set; } = new();

    }
}
