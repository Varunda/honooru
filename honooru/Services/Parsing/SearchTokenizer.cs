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

            bool inQuote = false;
            string word = "";

            foreach (char i in q) {

                _Logger.LogTrace($"tokenize iteration [iter={i}] [word={word}]");

                // if in a quote, only stop for another quote
                // TODO: support escaped quotes?
                if (inQuote == true) {
                    if (i != '"') {
                        word += i;
                        continue;
                    }
                }

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
                    // if a word has already started, assume the dash is part of the WORD, not the start of a NOT token
                    if (word.Length > 0) {
                        word += i;
                    } else {
                        // otherwise start a NOT token
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
                } else if (i == '"') {
                    if (inQuote == true) {
                        _Logger.LogTrace($"ending quoted strong");
                        inQuote = false;
                    } else {
                        _Logger.LogTrace($"starting quoted string");
                        inQuote = true;
                    }

                    if (word.Length > 0) {
                        tokens.Add(new Token(TokenType.WORD, word));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                        word = "";
                    }

                } else {
                    word += i;
                }
            }

            if (word.Length > 0) {
                tokens.Add(new Token(TokenType.WORD, word));
            }

            tokens.Add(new Token(TokenType.END, ""));

            if (inQuote == true) {
                throw new System.Exception($"missing end quote");
            }

            return tokens;
        }

    }
}
