using honooru.Code.Exceptions;
using honooru.Models.Search;
using honooru.Services.Db;
using honooru.Services.Parsing;
using honooru.Services.Repositories;
using honooru.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Services.Parsing {

    [TestClass]
    public class SearchQueryParserTest {

        /// <summary>
        ///     test invalid inputs to make sure they throw an exception.
        ///     the code expects an <see cref="AstParseException"/> to be thrown
        /// </summary>
        /// <param name="input">bad input</param>
        [DataTestMethod]
        [DataRow("{ { hi howdy")]
        [DataRow("{}")]
        [DataRow("{ hi howdy }")]
        [DataRow("{ - }")]
        [DataRow("{ } }")]
        [DataRow("}")]
        [DataRow("")]
        [DataRow("{~}")]
        [DataRow("{{{{{{{{{{{{{{{{{{{{{{")]
        [DataRow("--hi")]
        [DataRow("--{}{}{hi")]
        [DataRow("} oops ~ hi {")]
        [DataRow("{ oops ~ hi")]
        [DataRow("{ ~ ~ }")]
        [DataRow("{ hi ~ ~ ")]
        [DataRow(" { } ~ hi ")]
        public void SearchQueryParserTest_Parse_Fail_Or(string input) {
            try {
                Ast ast = _p().Parse(input);
                // we expect the tokens coming in to be bad, so an exception is thrown before this Fail()s
                Assert.Fail();
            } catch (AstParseException ex) {
                Console.WriteLine($"parse exception caught [ex={ex.Message}] [erroringToken={ex.ErroringToken}]");
            }
        }

        /// <summary>
        ///     test invalid inputs to make sure they throw an exception.
        ///     the code expects an <see cref="AstParseException"/> to be thrown
        /// </summary>
        /// <param name="input">bad input</param>
        [DataTestMethod]
        [DataRow("hi user:-apple")]
        [DataRow(" hi user : { hi }")]
        [DataRow("hi {howdy user:apple}")]
        public void SearchQueryParserTest_Parse_Fail_Meta(string input) {
            try {
                Ast ast = _p().Parse(input);
                // we expect the tokens coming in to be bad, so an exception is thrown before this Fail()s
                Assert.Fail();
            } catch (AstParseException ex) {
                Console.WriteLine($"parse exception caught [ex={ex.Message}] [erroringToken={ex.ErroringToken}]");
            }
        }

        private SearchQueryParser _p() {
            SearchTokenizer tokenizer = new SearchTokenizer(new TestLogger<SearchTokenizer>());
            AstBuilder astBuilder = new AstBuilder(new TestLogger<AstBuilder>());

            SearchQueryParser parser = new SearchQueryParser(
                logger: new TestLogger<SearchQueryParser>(),
                tokenizer: tokenizer,
                astBuilder: astBuilder
            );

            return parser;
        }


    }
}
