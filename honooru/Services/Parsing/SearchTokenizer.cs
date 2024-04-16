using honooru.Models.Search;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace honooru.Services.Parsing {

    public class SearchTokenizer {

        private readonly ILogger<SearchTokenizer> _Logger;

        public SearchTokenizer(ILogger<SearchTokenizer> logger) {
            _Logger = logger;
        }

        public List<Token> Tokenize(string q) {
            _Logger.LogDebug($"starting new tokenizing [q={q}]");

            List<Token> tokens = new();

            string word = "";

            foreach (char i in q) {

                _Logger.LogTrace($"tokenize iteration [iter={i}] [word={word}]");

                if (i == ' ') {
                    _Logger.LogTrace($"got space character");
                    if (word.Length > 0) {
                        tokens.Add(new Token(TokenType.WORD, word));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                        word = "";
                    }
                } else if (i == '{') {
                    if (word.Length > 0) {
                        tokens.Add(new Token(TokenType.WORD, word));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                        word = "";
                    }

                    tokens.Add(new Token(TokenType.OR_START, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                } else if (i == '}') {
                    if (word.Length > 0) {
                        tokens.Add(new Token(TokenType.WORD, word));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                        word = "";
                    }

                    tokens.Add(new Token(TokenType.OR_END, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                } else if (i == '~') {
                    if (word.Length > 0) {
                        tokens.Add(new Token(TokenType.WORD, word));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                        word = "";
                    }

                    tokens.Add(new Token(TokenType.OR_CONTINUE, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                } else if (i == ':') {
                    if (word.Length > 0) {
                        tokens.Add(new Token(TokenType.WORD, word));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                        word = "";
                    }

                    tokens.Add(new Token(TokenType.META, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                } else if (i == '-') {
                    if (word.Length > 0) {
                        word += i;
                        //tokens.Add(new Token(TokenType.WORD, word));
                        //word = "";
                    } else {
                        tokens.Add(new Token(TokenType.NOT, ""));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                    }
                } else if (i == '<' || i == '>' || i == '=' || i == '!') {
                    if (word.Length > 0) {
                        tokens.Add(new Token(TokenType.WORD, word));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                        word = "";
                    }

                    tokens.Add(new Token(TokenType.OPERATOR, "" + i));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                } else {
                    word += i;
                }
            }

            if (word.Length > 0) {
                tokens.Add(new Token(TokenType.WORD, word));
            }

            tokens.Add(new Token(TokenType.END, ""));

            return tokens;
        }

    }
}
