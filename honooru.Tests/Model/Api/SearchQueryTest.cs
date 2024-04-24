using honooru.Models.Api;
using honooru.Models.Search;
using honooru.Services.Parsing;
using honooru.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Model.Api {

    [TestClass]
    public class SearchQueryTest {

        /// <summary>
        ///     test that the <see cref="SearchQuery.HashKey"/> value is the same with the same queries
        /// </summary>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        [DataTestMethod]
        [DataRow("hi hello -howdy {tag_a ~ tag_b}", "{tag_b ~ tag_a} hello -howdy hi")]
        [DataRow("hi -howdy hello {tag_a ~ tag_b~tag_c}", "-howdy hello hi {tag_c               ~ tag_b ~tag_a}")]
        public void SearchQueryTest_CheckHashKey(string input, string input2) {
            AstBuilder builder = new(new TestLogger<AstBuilder>(false));
            SearchTokenizer tokenizer = new(new TestLogger<SearchTokenizer>(false));

            SearchQuery q1 = new(builder.Build(tokenizer.Tokenize(input)));
            SearchQuery q2 = new(builder.Build(tokenizer.Tokenize(input2)));

            Console.WriteLine($"q1 [hash={q1.HashKey}] [ast={q1.QueryAst.Print()}]");
            Console.WriteLine($"q2 [hash={q2.HashKey}] [ast={q2.QueryAst.Print()}]");

            Assert.AreEqual(q1.HashKey, q2.HashKey);
        }

        /// <summary>
        ///     test that <see cref="SearchQuery.HashKey"/> is different for not same queries
        /// </summary>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        [DataTestMethod]
        [DataRow("hi hello -howdy {tag_a ~ tag_b}", "hello2 -howdy hi {tag_b ~ tag_a}")]
        [DataRow("tag_a tag_b {hi ~ hello}", "tag_b {howdy ~ hi} tag_a")]
        public void SearchQueryTest_CheckHashKey_NotEqual(string input, string input2) {
            AstBuilder builder = new(new TestLogger<AstBuilder>(false));
            SearchTokenizer tokenizer = new(new TestLogger<SearchTokenizer>(false));

            SearchQuery q1 = new(builder.Build(tokenizer.Tokenize(input)));
            SearchQuery q2 = new(builder.Build(tokenizer.Tokenize(input2)));

            Console.WriteLine($"q1 [hash={q1.HashKey}] [ast={q1.QueryAst.Print()}]");
            Console.WriteLine($"q2 [hash={q2.HashKey}] [ast={q2.QueryAst.Print()}]");

            Assert.AreNotEqual(q1.HashKey, q2.HashKey);
        }

        /// <summary>
        ///     test to make sure that limits and offset don't affect the hash key of a search query
        /// </summary>
        /// <param name="input"></param>
        /// <param name="offset1"></param>
        /// <param name="offset2"></param>
        /// <param name="limit1"></param>
        /// <param name="limit2"></param>
        [DataTestMethod]
        [DataRow("hi hello -howdy {tag_a ~ tag_b}", (uint)1, (uint)10, (uint)5, (uint)50)]
        public void SearchQueryTest_CheckHashKey_DifferentOffsetOrLimit(string input, uint offset1, uint offset2, uint limit1, uint limit2) {
            AstBuilder builder = new(new TestLogger<AstBuilder>(false));
            SearchTokenizer tokenizer = new(new TestLogger<SearchTokenizer>(false));

            SearchQuery q1 = new(builder.Build(tokenizer.Tokenize(input)));
            q1.Offset = q1.Limit = 0;

            SearchQuery q2 = new(builder.Build(tokenizer.Tokenize(input)));
            q2.Offset = offset1;
            q2.Limit = 0;

            SearchQuery q3 = new(builder.Build(tokenizer.Tokenize(input)));
            q3.Offset = offset1;
            q3.Limit = limit1;

            SearchQuery q4 = new(builder.Build(tokenizer.Tokenize(input)));
            q4.Offset = offset1;
            q4.Limit = limit2;

            Console.WriteLine($"q1 [hash={q1.HashKey}] [ast={q1.QueryAst.Print()}]");
            Console.WriteLine($"q2 [hash={q2.HashKey}] [ast={q2.QueryAst.Print()}]");
            Console.WriteLine($"q3 [hash={q3.HashKey}] [ast={q3.QueryAst.Print()}]");
            Console.WriteLine($"q4 [hash={q4.HashKey}] [ast={q4.QueryAst.Print()}]");

            Assert.AreEqual(q1.HashKey, q2.HashKey);
            Assert.AreEqual(q1.HashKey, q3.HashKey);
            Assert.AreEqual(q1.HashKey, q4.HashKey);
            Assert.AreEqual(q2.HashKey, q3.HashKey);
            Assert.AreEqual(q2.HashKey, q4.HashKey);
            Assert.AreEqual(q3.HashKey, q4.HashKey);
        }

        
    }
}
