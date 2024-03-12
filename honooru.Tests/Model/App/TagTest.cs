using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Models.App;

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
        public void Test_Valid(string tag, bool expected) {
            Assert.AreEqual(Tag.Validate(tag), expected);
        }

    }
}
