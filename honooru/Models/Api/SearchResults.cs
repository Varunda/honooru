using honooru.Models.Db;
using System.Collections.Generic;

namespace honooru.Models.Api {

    public class SearchResults {

        public SearchResults(SearchQuery query) {
            Query = query;
            ParsedAst = query.QueryAst.Print();
        }

        /// <summary>
        ///     query performed
        /// </summary>
        public SearchQuery Query { get; set; }

        /// <summary>
        ///     debug info about how long some steps take to perform
        /// </summary>
        public List<string> Timings { get; set; } = new();

        /// <summary>
        ///     string representing the parsed AST
        /// </summary>
        public string ParsedAst { get; set; } = "";

        /// <summary>
        ///     results
        /// </summary>
        public List<Post> Results { get; set; } = new();

        /// <summary>
        ///     how many posts are in the query
        /// </summary>
        public int PostCount { get; set; }

        /// <summary>
        ///     tags that the <see cref="Post"/>s within <see cref="Results"/> have.
        ///     will be empty unless populated externally (the API handles this)
        /// </summary>
        public List<ExtendedTag> Tags { get; set; } = new();

    }
}
