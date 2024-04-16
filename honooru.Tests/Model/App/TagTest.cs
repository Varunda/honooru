using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Models.App;
using honooru.Services.Repositories;
using honooru.Tests.Util;
using Microsoft.Extensions.Caching.Memory;
using honooru.Services.Db;
using honooru.Services.Util;

namespace honooru.Tests {

    [TestClass]
    public class TagTest {

        [DataTestMethod]
        [DataRow("abc", true)]
        [DataRow("!abc", true)]
        [DataRow("abcdefghijklmnopqrstuvwxyz", true)]
        [DataRow("0123456789", true)]
        [DataRow("()", false)]
        [DataRow("@#$%^&*", false)]
        [DataRow("<>/?\"'{}\\|", false)]
        [DataRow("hi\0hehe", false)]
        [DataRow("abc_()", false)]
        [DataRow("abc_(abc)", true)]
        [DataRow("hi>yo", false)]
        [DataRow("hi=yo", false)]
        [DataRow("user:alice", false)]
        [DataRow("ns-44_commisioner", true)]
        [DataRow("-ns-44_commisioner", false)]
        public void Test_Valid(string tag, bool expected) {
            TestLogger<TagValidationService> logger = new TestLogger<TagValidationService>();
            TagValidationService validService = new TagValidationService(logger);

            TagNameValidationResult result = validService.ValidateTagName(tag);

            if (result.Valid == false) {
                Console.WriteLine($"input '{tag}' is invalid due to: {result.Reason}");
            }

            Assert.AreEqual(expected, result.Valid);
        }

    }
}
