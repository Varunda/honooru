namespace honooru.Models.Search {

    public class Token {

        public Token(TokenType type, string value) {
            Type = type;
            Value = value;
        }

        public TokenType Type { get; set; }

        public string Value { get; set; }

        public override string ToString() {
            return $"<{nameof(Token)} [{nameof(Type)}={Type}] [{nameof(Value)}='{Value}']>";
        }

    }

    public enum TokenType {

        DEFAULT = 0,

        WORD, // a string of characters that can be used in a tag

        NOT, // !

        OR_START, // {

        OR_CONTINUE, // ~

        OR_END, // }

        META, // :

        END, // end of token stream

        OPERATOR, // <>=!

    }

}
