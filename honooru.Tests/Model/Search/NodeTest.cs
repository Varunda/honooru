using honooru.Models.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Model.Search {

    [TestClass]
    public class NodeTest {

        private static Token _T(string value) => new Token(TokenType.DEFAULT, value);

        /// <summary>
        ///     test that the Depth property if a node is correct
        /// </summary>
        [TestMethod]
        public void NodeTest_Depth() {

            Node one = new Node(NodeType.TAG, _T("0"), null);
            Node two = new Node(NodeType.TAG, _T("1"), one);
            Node two_b = new Node(NodeType.TAG, _T("2"), one);
            Node three = new Node(NodeType.TAG, _T("3"), two);
            Node four = new Node(NodeType.TAG, _T("4"), three);
            Node five = new Node(NodeType.TAG, _T("5"), four);
            Node six = new Node(NodeType.TAG, _T("6"), five);

            Assert.AreEqual(0, one.Depth);
            Assert.AreEqual(1, two.Depth);
            Assert.AreEqual(1, two_b.Depth);
            Assert.AreEqual(2, three.Depth);
            Assert.AreEqual(3, four.Depth);
            Assert.AreEqual(4, five.Depth);
            Assert.AreEqual(5, six.Depth);

            // Depth is a cached value, ensure that once cached, the value is still correct
            Assert.AreEqual(0, one.Depth);
            Assert.AreEqual(1, two.Depth);
            Assert.AreEqual(1, two_b.Depth);
            Assert.AreEqual(2, three.Depth);
            Assert.AreEqual(3, four.Depth);
            Assert.AreEqual(4, five.Depth);
            Assert.AreEqual(5, six.Depth);

        }

        /// <summary>
        ///     test the GetRoot() extension method returns the correct node
        /// </summary>
        [TestMethod]
        public void NodeTest_Root() {

            Node one = new Node(NodeType.TAG, _T("0"), null);
            Node two = new Node(NodeType.TAG, _T("1"), one);
            Node two_b = new Node(NodeType.TAG, _T("2"), one);
            Node three = new Node(NodeType.TAG, _T("3"), two);
            Node four = new Node(NodeType.TAG, _T("4"), three);
            Node five = new Node(NodeType.TAG, _T("5"), four);
            Node six = new Node(NodeType.TAG, _T("6"), five);

            Assert.AreEqual(one, six.GetRoot());
            Assert.AreEqual(one, five.GetRoot());
            Assert.AreEqual(one, four.GetRoot());
            Assert.AreEqual(one, three.GetRoot());
            Assert.AreEqual(one, two.GetRoot());
            Assert.AreEqual(one, two_b.GetRoot());
            Assert.AreEqual(one, one.GetRoot());
        }

    }
}
