using honooru.Models.Search;
using honooru.Services.Parsing;
using honooru.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Services.Parsing {

    [TestClass]
    public class SearchTokenizerTest {

        /// <summary>
        ///     test that the AND operator is parsed correctly and ignores whitespace as needed
        /// </summary>
        /// <param name="input"></param>
        [DataTestMethod]
        [DataRow("hi howdy")]
        [DataRow("hi  howdy")]
        [DataRow(" hi howdy")]
        [DataRow("hi howdy ")]
        [DataRow(" hi  howdy")]
        [DataRow(" hi  howdy ")]
        [DataRow("                    hi                         howdy                         ")]
        public void SearchTokenizerTest_And(string input) {
            List<Token> tokens = _t(input);

            Assert.AreEqual(3, tokens.Count); // include the END token

            List<Token> expected = new() {
                new Token(TokenType.WORD, "hi"),
                new Token(TokenType.WORD, "howdy"),
                new Token(TokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test that the OR operator is parsed correctly and ignores whitespace as needed
        /// </summary>
        /// <param name="input">input query</param>
        [DataTestMethod]
        [DataRow("{hi ~ howdy} hello")]
        [DataRow(" { hi ~ howdy }hello")]
        [DataRow("{hi~howdy}hello")]
        [DataRow("{hi~         howdy }hello")]
        [DataRow("{hi                    ~howdy                       }                    hello")]
        public void SearchTokenizerTest_Or(string input) {
            List<Token> tokens = _t(input);
            Assert.AreEqual(7, tokens.Count);

            List<Token> expected = new List<Token>() {
                new Token(TokenType.OR_START, ""),
                new Token(TokenType.WORD, "hi"),
                new Token(TokenType.OR_CONTINUE, ""),
                new Token(TokenType.WORD, "howdy"),
                new Token(TokenType.OR_END, ""),
                new Token(TokenType.WORD, "hello"),
                new Token(TokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test that the NOT operator is parsed correctly and ignores whitespace as needed
        /// </summary>
        /// <param name="input"></param>
        [DataTestMethod]
        [DataRow("-hello")]
        [DataRow(" - hello ")]
        public void SearchTokenizerTest_Not(string input) {
            List<Token> tokens = _t(input);
            Assert.AreEqual(3, tokens.Count);

            List<Token> expected = new List<Token>() {
                new Token(TokenType.NOT, ""),
                new Token(TokenType.WORD, "hello"),
                new Token(TokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test the combo of NOT and AND operators
        /// </summary>
        /// <param name="input"></param>
        [DataTestMethod]
        [DataRow("hi -hello")]
        [DataRow(" hi -hello")]
        [DataRow(" hi - hello ")]
        public void SearchTokenizerTest_AndWithNot(string input) {
            List<Token> tokens = _t(input);
            Assert.AreEqual(4, tokens.Count);

            List<Token> expected = new List<Token>() {
                new Token(TokenType.WORD, "hi"),
                new Token(TokenType.NOT, ""),
                new Token(TokenType.WORD, "hello"),
                new Token(TokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        [TestMethod]
        public void SearchTokenizerTest_EmptyInput() {
            List<Token> tokens = _t("");

            Assert.AreEqual(1, tokens.Count);

            List<Token> expected = new List<Token>() {
                new Token(TokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        [TestMethod]
        public void SearchTokenizerTest_DoubleOr() {
            List<Token> tokens = _t("{{}}");

            Assert.AreEqual(5, tokens.Count);

            List<Token> expected = new List<Token>() {
                new Token(TokenType.OR_START, ""),
                new Token(TokenType.OR_START, ""),
                new Token(TokenType.OR_END, ""),
                new Token(TokenType.OR_END, ""),
                new Token(TokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        [DataTestMethod]
        [DataRow("user:apple")]
        [DataRow("user: apple")]
        [DataRow(" user:apple")]
        [DataRow("user :apple")]
        [DataRow(" user: apple")]
        [DataRow(" user : apple")]
        public void SearchTokenizerTest_Meta(string input) {
            List<Token> tokens = _t(input);

            Assert.AreEqual(4, tokens.Count);

            List<Token> expected = new List<Token>() {
                new Token(TokenType.WORD, "user"),
                new Token(TokenType.META, ""),
                new Token(TokenType.WORD, "apple"),
                new Token(TokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        [DataTestMethod]
        [DataRow("user:>apple")]
        [DataRow("user: >apple")]
        [DataRow(" user:>apple")]
        [DataRow("user :>apple")]
        [DataRow(" user: >apple")]
        [DataRow(" user : >apple")]
        [DataRow("user:> apple")]
        [DataRow("user: > apple")]
        [DataRow(" user:> apple")]
        [DataRow("user :> apple")]
        [DataRow(" user: > apple")]
        [DataRow(" user : > apple")]
        [DataRow("user:>apple ")]
        [DataRow("user: >apple ")]
        [DataRow(" user:>apple ")]
        [DataRow("user :>apple ")]
        [DataRow(" user: >apple ")]
        [DataRow(" user : >apple ")]
        [DataRow("user:> apple ")]
        [DataRow("user: > apple ")]
        [DataRow(" user:> apple ")]
        [DataRow("user :> apple ")]
        [DataRow(" user: > apple ")]
        [DataRow(" user : > apple ")]
        public void SearchTokenizerTest_MetaWithOperator(string input) {
            List<Token> tokens = _t(input);

            Assert.AreEqual(5, tokens.Count);

            List<Token> expected = new List<Token>() {
                new Token(TokenType.WORD, "user"),
                new Token(TokenType.META, ""),
                new Token(TokenType.OPERATOR, ">"),
                new Token(TokenType.WORD, "apple"),
                new Token(TokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     (c)ompare a list of tokens from the tokenizer and the expected tokens
        /// </summary>
        /// <param name="tokens">tokens from the tokenizer</param>
        /// <param name="expected">
        ///     hand-constructed array of tokens we expect to get.
        ///     this is an array to prevent mixing of the two parameters
        /// </param>
        private void _c(List<Token> tokens, Token[] expected) {
            Assert.AreEqual(expected.Length, tokens.Count); // double check the expected value is correct

            for (int i = 0; i < expected.Length; ++i) {
                Token actual = tokens.ElementAt(i);
                Token wanted = expected[i];

                Assert.AreEqual(wanted.Type, actual.Type);
                Assert.AreEqual(wanted.Value, actual.Value);
            }
        }

        /// <summary>
        ///     create a <see cref="SearchTokenizer"/> with a logger that prints to console,
        ///     and return the <see cref="Token"/>s it generates
        /// </summary>
        /// <param name="q">input search query</param>
        /// <returns>
        ///     a list of <see cref="Token"/>s
        /// </returns>
        private List<Token> _t(string q) {
            TestLogger<SearchTokenizer> logger = new TestLogger<SearchTokenizer>();

            SearchTokenizer tokenizer = new(logger);

            List<Token> tokens = tokenizer.Tokenize(q);

            Console.WriteLine($"got {tokens.Count} tokens from '{q}'");
            foreach (Token t in tokens) {
                Console.Write($"\t{t.Type}");
                if (t.Type == TokenType.WORD) {
                    Console.Write($"={t.Value}");
                }
                Console.WriteLine();
            }

            return tokens;
        }

    }
}
