using honooru.Models.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Model.Search {

    [TestClass]
    public class TokenStreamTest {

        [TestMethod]
        public void TokenStreamTest_ForEach() {
            List<Token> tokens = new() {
                new Token(TokenType.WORD, "1"),
                new Token(TokenType.WORD, "2"),
                new Token(TokenType.WORD, "3"),
                new Token(TokenType.WORD, "4"),
                new Token(TokenType.WORD, "5"),
                new Token(TokenType.WORD, "6"),
            };

            TokenStream stream = new(tokens);

            Assert.AreEqual(TokenStream.DEFAULT_TOKEN, stream.Current);

            int i = 1;
            foreach (Token token in tokens) {
                Assert.AreEqual(i, int.Parse(token.Value));
                ++i;
            }
        }

        [TestMethod]
        public void TokenStreamTest_Iteration() {

            List<Token> tokens = new() {
                new Token(TokenType.WORD, "1"),
                new Token(TokenType.WORD, "2"),
                new Token(TokenType.WORD, "3"),
                new Token(TokenType.WORD, "4"),
                new Token(TokenType.WORD, "5"),
                new Token(TokenType.WORD, "6"),
            };

            TokenStream stream = new(tokens);

            Assert.AreEqual(TokenStream.DEFAULT_TOKEN, stream.Current);

            Assert.IsTrue(stream.MoveNext());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("1", stream.Current.Value);

            Assert.IsTrue(stream.MoveNext());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("2", stream.Current.Value);

            Assert.IsTrue(stream.MoveNext());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("3", stream.Current.Value);

            Assert.IsTrue(stream.MoveNext());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("4", stream.Current.Value);

            Assert.IsTrue(stream.MoveNext());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("5", stream.Current.Value);

            Assert.IsTrue(stream.MoveNext());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("6", stream.Current.Value);

            Assert.IsFalse(stream.MoveNext());
            Assert.AreEqual(TokenStream.DEFAULT_TOKEN, stream.Current);

            Assert.IsTrue(stream.MoveBack());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("6", stream.Current.Value);

            Assert.IsTrue(stream.MoveBack());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("5", stream.Current.Value);

            Assert.IsTrue(stream.MoveBack());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("4", stream.Current.Value);

            Assert.IsTrue(stream.MoveBack());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("3", stream.Current.Value);

            Assert.IsTrue(stream.MoveBack());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("2", stream.Current.Value);

            Assert.IsTrue(stream.MoveBack());
            Assert.AreEqual(TokenType.WORD, stream.Current.Type);
            Assert.AreEqual("1", stream.Current.Value);

            Assert.IsFalse(stream.MoveBack());
            Assert.AreEqual(TokenStream.DEFAULT_TOKEN, stream.Current);

        }

        [TestMethod]
        public void TokenStreamTest_GetNext() {
            List<Token> tokens = new() {
                new Token(TokenType.WORD, "1"),
                new Token(TokenType.WORD, "2")
            };

            TokenStream stream = new(tokens);

            Assert.AreEqual(TokenStream.DEFAULT_TOKEN, stream.Current);
            Assert.IsNotNull(stream.GetNext());
            Assert.IsNotNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
            Assert.IsNull(stream.GetNext());
        }

        [TestMethod]
        public void TokenStreamTest_PeekNext() {
            List<Token> tokens = new() {
                new Token(TokenType.WORD, "1"),
                new Token(TokenType.WORD, "2")
            };

            TokenStream stream = new(tokens);

            Assert.AreEqual(TokenStream.DEFAULT_TOKEN, stream.Current);
            Assert.IsNotNull(stream.PeekNext());
            Assert.AreEqual("1", stream.PeekNext()!.Value);
            Assert.IsNotNull(stream.PeekNext());
            Assert.AreEqual("1", stream.PeekNext()!.Value);
            Assert.IsTrue(stream.MoveNext());
            Assert.IsNotNull(stream.PeekNext());
            Assert.AreEqual("2", stream.PeekNext()!.Value);
            Assert.IsNotNull(stream.PeekNext());
            Assert.AreEqual("2", stream.PeekNext()!.Value);
            Assert.IsTrue(stream.MoveNext());
            Assert.IsNull(stream.PeekNext());
            Assert.IsNull(stream.PeekNext());

        }

    }
}
