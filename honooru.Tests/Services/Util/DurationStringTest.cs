using honooru.Services.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Services.Util {

    [TestClass]
    public class DurationStringTest {

        [DataTestMethod]
        [DataRow("1h", 1000 * 60 * 60)]
        [DataRow("1h4m", 1000 * 60 * 60 + (1000 * 60 * 4))]
        [DataRow("1h4m40s", 1000 * 60 * 60 + (1000 * 60 * 4) + (1000 * 40))]
        [DataRow("1d1h4m40s", (1000 * 60 * 60 * 24) + (1000 * 60 * 60) + (1000 * 60 * 4) + (1000 * 40))]
        [DataRow("1D1H4M40s", (1000 * 60 * 60 * 24) + (1000 * 60 * 60) + (1000 * 60 * 4) + (1000 * 40))] // test that upper case and lower case gives the same results
        [DataRow("01D01H000004M40s", (1000 * 60 * 60 * 24) + (1000 * 60 * 60) + (1000 * 60 * 4) + (1000 * 40))] // test that leading zeroes are ignored
        public void DurationStringTest_Valid(string input, long expectedMs) {
            TimeSpan expected = TimeSpan.FromMilliseconds(expectedMs);

            TimeSpan? actual = DurationStringUtil.Parse(input);

            Console.WriteLine($"input={input}, expected={expected}, actual={actual}");
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual.Value);
        }

    }
}
