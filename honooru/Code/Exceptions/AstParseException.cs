using honooru.Models.Search;
using System;

namespace honooru.Code.Exceptions {

    public class AstParseException : Exception {

        public Token? ErroringToken { get; set; }

        public AstParseException(Token? erroringToken) : base() {
            ErroringToken = erroringToken;
        }

        public AstParseException(Token? erroringToken, string message) : base(message) {
            ErroringToken = erroringToken;
        }

        public AstParseException(Token? erroringToken, string message, Exception? inner) : base(message, inner) {
            ErroringToken = erroringToken;
        }

    }
}
