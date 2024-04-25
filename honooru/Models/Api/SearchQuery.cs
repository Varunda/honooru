using honooru.Models.Search;
using System.Collections.Generic;

namespace honooru.Models.Api {

    public class SearchQuery {

        public SearchQuery(Ast ast) {
            QueryAst = ast;
        }

        public Ast QueryAst { get; set; }

        public uint Offset { get; set; }

        public uint Limit { get; set; }

        public bool IsRandom { get; set; } = false;

        public string HashKey {
            get {
                return $"ast_{QueryAst.GetHashCode()}";
            }
        }

    }

}
