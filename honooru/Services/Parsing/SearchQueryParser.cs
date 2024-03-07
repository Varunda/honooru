using honooru.Models.Search;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace honooru.Services.Parsing {

    public class SearchQueryParser {

        private readonly ILogger<SearchQueryParser> _Logger;

        private readonly SearchTokenizer _Tokenizer;
        private readonly AstBuilder _AstBuilder;

        public SearchQueryParser(ILogger<SearchQueryParser> logger,
            SearchTokenizer tokenizer, AstBuilder astBuilder) {

            _Logger = logger;

            _Tokenizer = tokenizer;
            _AstBuilder = astBuilder;
        }

        public Ast Parse(string input) {
            _Logger.LogDebug($"tokenizing input [input='{input}']");

            List<Token> tokens = _Tokenizer.Tokenize(input);
            _Logger.LogDebug($"tokenized input [tokens.Count={tokens.Count}] [input='{input}']");
            _Logger.LogTrace($"tokens parsed [{string.Join(" ", tokens)}]");

            Ast ast = _AstBuilder.Build(tokens);
            _Logger.LogDebug($"generated AST [ast={ast.Print()}]");

            return ast;
        }

    }
}
