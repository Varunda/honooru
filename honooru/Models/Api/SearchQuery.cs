using honooru.Models.Search;
using System.Collections.Generic;

namespace honooru.Models.Api {

    public class SearchQuery {

        public SearchQuery(Ast ast) {
            QueryAst = ast;
        }

        public string Input { get; set; } = "";

        public Ast QueryAst { get; set; }

        public uint Offset { get; set; }

        public uint Limit { get; set; }

    }

}
