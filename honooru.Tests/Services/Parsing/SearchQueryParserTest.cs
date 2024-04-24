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
        [DataRow("{ { hi howdy")] // invalid due to missing OR_END }
        [DataRow("{}")] // invalid due to empty OR
        [DataRow("{ hi howdy }")] // invalid due to no OR_CONTINUE (~) between tags
        [DataRow("{ - }")] // invalid due to - not being a valid tag
        [DataRow("{ } }")] // invalid due to an unexpected OR_END
        [DataRow("}")] // missing a starting }
        [DataRow("")] // empty
        [DataRow("{~}")] // missing any tags
        [DataRow("{{{{{{{{{{{{{{{{{{{{{{")] // too many ORs
        [DataRow("--hi")] // double negative is bad
        [DataRow("--{}{}{hi")] // just bad
        [DataRow("} oops ~ hi {")] // swapped OR_START and OR_END
        [DataRow("{ oops ~ hi")]  // missing OR_END
        [DataRow("{ ~ ~ }")] // missing tags
        [DataRow("{ hi ~ ~ ")] // not enough tags inbetween OR_CONTINUE
        [DataRow(" { } ~ hi ")] // no tags within the OR
        [DataRow(" { user: ~ }")] // missing a meta value
        [DataRow(" { user:hi ~ }")] // missing another tag
        [DataRow(" { user: ~ }")] // missing a meta value
        [DataRow(" { user: ~ }")] // missing a meta value
        [DataRow(" { user: ~ }")] // missing a meta value
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
        [DataRow("hi {user:apple howdy}")]
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
