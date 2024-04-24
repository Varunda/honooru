using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace honooru.Models.Search {

    public class TokenStream : IEnumerable<Token>, IEnumerator<Token> {

        public static readonly Token DEFAULT_TOKEN = new Token(TokenType.DEFAULT, "");

        private readonly List<Token> _Tokens;

        private int _Index = -1;

        private Token _Current = DEFAULT_TOKEN;

        public TokenStream(List<Token> tokens) {
            _Tokens = tokens;
        }

        public Token Current => _Current ?? throw new InvalidOperationException($"cannot iterate");

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            if (_Index < _Tokens.Count - 1) {
                _Current = _Tokens.ElementAt(_Index + 1);
                ++_Index;
                return true;
            } else {
                _Current = DEFAULT_TOKEN;
                ++_Index;
                return false;
            }
        }

        /// <summary>
        ///     get the current token and advance to the next one
        /// </summary>
        /// <returns></returns>
        public Token? GetNext() {
            if (MoveNext()) {
                return Current;
            }
            return null;
        }

        /// <summary>
        ///     peek at the next token without removing it from the stream
        /// </summary>
        /// <returns></returns>
        public Token? PeekNext() {
            if (_Index + 1 >= _Tokens.Count) {
                return null;
            }
            return _Tokens.ElementAt(_Index + 1);
        }

        /// <summary>
        ///     move back if possible in the token stream
        /// </summary>
        /// <returns></returns>
        public bool MoveBack() {
            if (--_Index >= 0) {
                _Current = _Tokens.ElementAt(_Index);
                return true;
            } else {
                _Current = DEFAULT_TOKEN;
                return false;
            }
        }

        /// <summary>
        ///     move back and get the previous token
        /// </summary>
        /// <returns></returns>
        public Token? GetPrevious() {
            if (MoveBack()) {
                return Current;
            }
            return null;
        }

        public void Reset() {
            _Index = 0;
        }

        public IEnumerator<Token> GetEnumerator() => new List<Token>(_Tokens).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => new List<Token>(_Tokens).GetEnumerator();

        public void Dispose() { }

    }

}
