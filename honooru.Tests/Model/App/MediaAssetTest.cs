using honooru.Models.App;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Model.App {

    [TestClass]
    public class MediaAssetTest {

        [TestMethod]
        public void Test_MediaAssetEquality() {
            DateTime n = DateTime.UtcNow;

            MediaAsset m1 = new();
            m1.Timestamp = n;
            MediaAsset m2 = new();
            m2.Timestamp = n;

            Assert.IsTrue(m1 == m2);

            m2 = new(m1);
            Assert.IsTrue(m1 == m2);

            m2.FileName = "abc";
            Assert.IsFalse(m1 == m2);

            m2 = new(m1);
            m2.FileExtension = "mkv";
            Assert.IsFalse(m1 == m2);
        }

    }
}
