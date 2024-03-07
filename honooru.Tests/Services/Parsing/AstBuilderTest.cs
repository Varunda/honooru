using honooru.Code.Exceptions;
using honooru.Models.Search;
using honooru.Services.Parsing;
using honooru.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Services.Parsing {

    [TestClass]
    public class AstBuilderTest {

        /// <summary>
        ///     test a single term input
        /// </summary>
        [DataTestMethod]
        [DataRow("hi")]
        [DataRow("person_(artist)")]
        [DataRow("nason's_defiance")]
        [DataRow("juga_(outfit)_(briggs)")]
        [Timeout(5_000)]
        public void AstBuilderTest_SingleTerm(string input) {
            // hi
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.WORD, input)
            };

            Ast ast = _a(tokens);

            _CheckTagNodeValue(ast.Root, input);

            List<NodeIter> expected = new List<NodeIter>() {
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, input))
            };
            _CheckNodeOrder(ast, expected);
        }

        /// <summary>
        ///     test
        /// </summary>
        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_ToEnumerable_And() {
            // hi howdy hello
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.WORD, "hi"),
                new Token(TokenType.WORD, "howdy"),
                new Token(TokenType.WORD, "hello")
            };

            Ast ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                NodeType.AND,
                new() {
                    (node) => _CheckTagNodeValue(node, "hi"),
                    (node) => _CheckTagNodeValue(node, "howdy"),
                    (node) => _CheckTagNodeValue(node, "hello")
                }
            );

            List<NodeIter> expected = new List<NodeIter>() {
                new NodeIter(NodeType.AND, new Token(TokenType.DEFAULT, "")),
                    new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hi")),
                    new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "howdy")),
                    new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hello")),
            };
            _CheckNodeOrder(ast, expected);
        }

        /// <summary>
        ///     test
        /// </summary>
        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_ToEnumerable_Or() {
            // {hi ~ hello}
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.OR_START, ""),
                    new Token(TokenType.WORD, "hi"),
                    new Token(TokenType.OR_CONTINUE, ""),
                    new Token(TokenType.WORD, "hello"),
                new Token(TokenType.OR_END, "")
            };

            Ast ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                NodeType.OR,
                new() {
                    (node) => _CheckTagNodeValue(node, "hi"),
                    (node) => _CheckTagNodeValue(node, "hello"),
                }
            );

            List<NodeIter> expected = new List<NodeIter>() {
                new NodeIter(NodeType.OR, new Token(TokenType.OR_START, "")),
                    new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hi")),
                    new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hello")),
            };
            _CheckNodeOrder(ast, expected);
        }

        /// <summary>
        ///     test
        /// </summary>
        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_ToEnumerable_AndWithOr_Simple() {
            // 1 {2 ~ 3}
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.WORD, "1"),
                new Token(TokenType.OR_START, ""),
                    new Token(TokenType.WORD, "2"),
                    new Token(TokenType.OR_CONTINUE, ""),
                    new Token(TokenType.WORD, "3"),
                new Token(TokenType.OR_END, "")
            };

            Ast ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                NodeType.AND,
                new List<Action<Node>>() {
                    (node) => _CheckTagNodeValue(node, "1"),
                    (node) => _CheckNodeTree(node, NodeType.OR, new() {
                        (node) => _CheckTagNodeValue(node, "2"),
                        (node) => _CheckTagNodeValue(node, "3"),
                    })
                }
            );
        }

        /// <summary>
        ///     test
        /// </summary>
        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_ToEnumerable_AndWithOr() {
            // 1 2 {3 ~ 4} 5 {6 ~ 7} 8
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.WORD, "1"),
                new Token(TokenType.WORD, "2"),
                new Token(TokenType.OR_START, ""),
                    new Token(TokenType.WORD, "3"),
                    new Token(TokenType.OR_CONTINUE, ""),
                    new Token(TokenType.WORD, "4"),
                new Token(TokenType.OR_END, ""),

                new Token(TokenType.WORD, "5"),

                new Token(TokenType.OR_START, ""),
                    new Token(TokenType.WORD, "6"),
                    new Token(TokenType.OR_CONTINUE, ""),
                    new Token(TokenType.WORD, "7"),
                new Token(TokenType.OR_END, ""),

                new Token(TokenType.WORD, "8")
            };

            Ast ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                NodeType.AND,
                new List<Action<Node>>() {
                    (node) => _CheckTagNodeValue(node, "1"),
                    (node) => _CheckTagNodeValue(node, "2"),
                    (node) => _CheckNodeTree(node, NodeType.OR, new List<Action<Node>>() {
                        (node) => _CheckTagNodeValue(node, "3"),
                        (node) => _CheckTagNodeValue(node, "4")
                    }),
                    (node) => _CheckTagNodeValue(node, "5"),
                    (node) => _CheckNodeTree(node, NodeType.OR, new List<Action<Node>>() {
                        (node) => _CheckTagNodeValue(node, "6"),
                        (node) => _CheckTagNodeValue(node, "7")
                    }),
                    (node) => _CheckTagNodeValue(node, "8")
                }
            );
        }

        /// <summary>
        ///     test
        /// </summary>
        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_ToEnumerable_Not() {
            // -1
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.NOT, ""),
                new Token(TokenType.WORD, "1"),
            };

            Ast ast = _a(tokens);

            _CheckTagNodeValue(ast.Root, "1");
        }

        /// <summary>
        ///     check that AND ASTs are built correctly
        /// </summary>
        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_Build_And() {
            // hi howdy hellow
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.WORD, "hi"),
                new Token(TokenType.WORD, "howdy"),
                new Token(TokenType.WORD, "hello")
            };

            Ast ast = _a(tokens);

            List<NodeIter> expected = new() {
                new NodeIter(NodeType.AND, new Token(TokenType.DEFAULT, "")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hi")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "howdy")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hello")),
            };

            _CheckNodeOrder(ast, expected);
        }

        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_Build_Or() {
            // { hi ~ howdy }
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.OR_START, ""),
                new Token(TokenType.WORD, "hi"),
                new Token(TokenType.OR_CONTINUE, ""),
                new Token(TokenType.WORD, "howdy"),
                new Token(TokenType.OR_END, "")
            };

            Ast ast = _a(tokens);
            Console.WriteLine($"parsed AST: {ast.Print()}");

            List<NodeIter> expected = new() {
                new NodeIter(NodeType.OR, new Token(TokenType.OR_START, "")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hi")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "howdy"))
            };

            _CheckNodeOrder(ast, expected);
        }

        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_Build_Not() {
            // -hi
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.NOT, ""),
                new Token(TokenType.WORD, "hi"),
            };

            Ast ast = _a(tokens);

            List<NodeIter> expected = new() {
                new NodeIter(NodeType.NOT_TAG, new Token(TokenType.WORD, "hi"))
            };

            _CheckNodeOrder(ast, expected);
        }

        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_Build_NotWithAnd() {
            // hi howdy -hello
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.WORD, "hi"),
                new Token(TokenType.WORD, "howdy"),
                new Token(TokenType.NOT, ""),
                new Token(TokenType.WORD, "hello"),
            };

            Ast ast = _a(tokens);

            List<NodeIter> expected = new() {
                new NodeIter(NodeType.AND, new Token(TokenType.DEFAULT, "")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hi")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "howdy")),
                new NodeIter(NodeType.NOT_TAG, new Token(TokenType.WORD, "hello")),
            };

            _CheckNodeOrder(ast, expected);
        }

        /// <summary>
        ///     test and OR with NOT after the OR
        /// </summary>
        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_Build_NotWithOr() {
            // { hi ~ hello } -howdy
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.OR_START, ""),
                new Token(TokenType.WORD, "hi"),
                new Token(TokenType.OR_CONTINUE, ""),
                new Token(TokenType.WORD, "hello"),
                new Token(TokenType.OR_END, ""),
                new Token(TokenType.NOT, ""),
                new Token(TokenType.WORD, "howdy"),
            };

            Ast ast = _a(tokens);

            List<NodeIter> expected = new() {
                new NodeIter(NodeType.AND, new Token(TokenType.DEFAULT, "")),
                new NodeIter(NodeType.OR, new Token(TokenType.OR_START, "")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hi")),
                new NodeIter(NodeType.TAG, new Token(TokenType.WORD, "hello")),
                new NodeIter(NodeType.NOT_TAG, new Token(TokenType.WORD, "howdy")),
            };

            _CheckNodeOrder(ast, expected);
        }

        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_Build_Meta() {
            // 1 2 user:alice
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.WORD, "1"),
                new Token(TokenType.WORD, "2"),
                new Token(TokenType.WORD, "user"),
                new Token(TokenType.META, ""),
                new Token(TokenType.WORD, "alice"),
                new Token(TokenType.WORD, "width"),
                new Token(TokenType.META, ""),
                new Token(TokenType.OPERATOR, ">"),
                new Token(TokenType.WORD, "1160"),
                new Token(TokenType.WORD, "height"),
                new Token(TokenType.META, ""),
                new Token(TokenType.OPERATOR, "="),
                new Token(TokenType.WORD, "63")
            };

            Ast ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                NodeType.AND,
                new List<Action<Node>>() {
                    (node) => _CheckTagNodeValue(node, "1"),
                    (node) => _CheckTagNodeValue(node, "2"),
                    (node) => _CheckNodeTree(node, NodeType.META, new List<Action<Node>>() {
                        (node) => _CheckNode(node, NodeType.META_FIELD, "user"),
                        (node) => _CheckNode(node, NodeType.META_OPERATOR, "="),
                        (node) => _CheckNode(node, NodeType.META_VALUE, "alice")
                    }),
                    (node) => _CheckNodeTree(node, NodeType.META, new List<Action<Node>>() {
                        (node) => _CheckNode(node, NodeType.META_FIELD, "width"),
                        (node) => _CheckNode(node, NodeType.META_OPERATOR, ">"),
                        (node) => _CheckNode(node, NodeType.META_VALUE, "1160")
                    }),
                    (node) => _CheckNodeTree(node, NodeType.META, new List<Action<Node>>() {
                        (node) => _CheckNode(node, NodeType.META_FIELD, "height"),
                        (node) => _CheckNode(node, NodeType.META_OPERATOR, "="),
                        (node) => _CheckNode(node, NodeType.META_VALUE, "63")
                    })
                }
            );
        }

        [TestMethod]
        [Timeout(5_000)]
        public void AstBuilderTest_Build_BigQuery() {
            // 1 2 user:alice width:>1160 height:=63 3 { 4 ~ 5 ~ 6 } score:>10 7
            List<Token> tokens = new List<Token>() {
                new Token(TokenType.WORD, "1"),
                new Token(TokenType.WORD, "2"),
                new Token(TokenType.WORD, "user"),
                new Token(TokenType.META, ""),
                new Token(TokenType.WORD, "alice"),
                new Token(TokenType.WORD, "width"),
                new Token(TokenType.META, ""),
                new Token(TokenType.OPERATOR, ">"),
                new Token(TokenType.WORD, "1160"),
                new Token(TokenType.WORD, "height"),
                new Token(TokenType.META, ""),
                new Token(TokenType.OPERATOR, "="),
                new Token(TokenType.WORD, "63"),
                new Token(TokenType.WORD, "3"),
                new Token(TokenType.OR_START, ""),
                new Token(TokenType.WORD, "4"),
                new Token(TokenType.OR_CONTINUE, ""),
                new Token(TokenType.WORD, "5"),
                new Token(TokenType.OR_CONTINUE, ""),
                new Token(TokenType.WORD, "6"),
                new Token(TokenType.OR_END, ""),
                new Token(TokenType.WORD, "score"),
                new Token(TokenType.META, ""),
                new Token(TokenType.OPERATOR, ">"),
                new Token(TokenType.WORD, "10"),
                new Token(TokenType.WORD, "7")
            };

            Ast ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                NodeType.AND,
                new List<Action<Node>>() {
                    (node) => _CheckTagNodeValue(node, "1"),
                    (node) => _CheckTagNodeValue(node, "2"),
                    (node) => _CheckNodeTree(node, NodeType.META, new List<Action<Node>>() {
                        (node) => _CheckNode(node, NodeType.META_FIELD, "user"),
                        (node) => _CheckNode(node, NodeType.META_OPERATOR, "="),
                        (node) => _CheckNode(node, NodeType.META_VALUE, "alice")
                    }),
                    (node) => _CheckNodeTree(node, NodeType.META, new List<Action<Node>>() {
                        (node) => _CheckNode(node, NodeType.META_FIELD, "width"),
                        (node) => _CheckNode(node, NodeType.META_OPERATOR, ">"),
                        (node) => _CheckNode(node, NodeType.META_VALUE, "1160")
                    }),
                    (node) => _CheckNodeTree(node, NodeType.META, new List<Action<Node>>() {
                        (node) => _CheckNode(node, NodeType.META_FIELD, "height"),
                        (node) => _CheckNode(node, NodeType.META_OPERATOR, "="),
                        (node) => _CheckNode(node, NodeType.META_VALUE, "63")
                    }),
                    (node) => _CheckTagNodeValue(node, "3"),
                    (node) => _CheckNodeTree(node, NodeType.OR, new List<Action<Node>>() {
                        (node) => _CheckTagNodeValue(node, "4"),
                        (node) => _CheckTagNodeValue(node, "5"),
                        (node) => _CheckTagNodeValue(node, "6"),
                    }),
                    (node) => _CheckNodeTree(node, NodeType.META, new List<Action<Node>>() {
                        (node) => _CheckNode(node, NodeType.META_FIELD, "score"),
                        (node) => _CheckNode(node, NodeType.META_OPERATOR, ">"),
                        (node) => _CheckNode(node, NodeType.META_VALUE, "10")
                    }),
                    (node) => _CheckTagNodeValue(node, "7")
                }
            );

            foreach (Node node in ast) {
                Console.WriteLine($"{new string('\t', node.Depth + 1)}{node.Type} {node.Token.Type} {node.Token.Value}");
            }
        }

        // ============================================================================================================
        // helper functions
        // ============================================================================================================

        /// <summary>
        ///     helper function to build an AST tree
        /// </summary>
        /// <param name="tokens">tokens used to build the AST</param>
        /// <returns></returns>
        private Ast _a(List<Token> tokens) {
            Console.WriteLine($"AST generating... from: [{string.Join(", ", tokens.Select(iter => $"{iter.Type}{(iter.Value != "" ? (":" + iter.Value): "")}"))}]");

            AstBuilder builder = new AstBuilder(new TestLogger<AstBuilder>());
            Ast ast = builder.Build(tokens);

            Console.WriteLine($"parsed AST: {ast.Print()}");

            return ast;
        }

        /// <summary>
        ///     helper function to validate the nodes in an AST. does NOT check children, ONLY the order
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="expected"></param>
        private void _CheckNodeOrder(Ast ast, List<NodeIter> expected) {

            int count = 0;
            IEnumerator<Node> astNodes = ast.GetEnumerator();
            while (astNodes.MoveNext()) {
                ++count;
            }

            Console.WriteLine($"have {count} nodes from the AST");

            Assert.AreEqual(expected.Count, count);

            int i = 0;
            Console.WriteLine($"nodes within AST tree:");
            foreach (Node node in ast) {
                Assert.IsTrue(i < expected.Count, $"i is {i}, and {i} < {expected.Count} is true, which means we've iterated past the end of the AST");

                NodeIter ex = expected.ElementAt(i);

                Assert.AreEqual(ex.Type, node.Type);
                Assert.AreEqual(ex.Token.Type, node.Token.Type);
                Assert.AreEqual(ex.Token.Value, node.Token.Value);

                Console.WriteLine($"{new string('\t', node.Depth + 1)}{node.Type} {node.Token.Type} {node.Token.Value}");

                ++i;
            }
        }

        private void _CheckNodeTree(Node node, NodeType type, List<Action<Node>> childrenChecks) {
            Assert.AreEqual(type, node.Type);
            Assert.AreEqual(childrenChecks.Count, node.Children.Count);

            for (int i = 0; i < node.Children.Count; ++i) {
                Node childNode = node.Children.ElementAt(i);
                Action<Node> check = childrenChecks.ElementAt(i);

                check(childNode);
            }
        }

        private void _CheckNode(Node node, NodeType type, string value) {
            Assert.AreEqual(type, node.Type);
            Assert.AreEqual(value, node.Token.Value);
            Assert.AreEqual(0, node.Children.Count);
        }

        private void _CheckTagNodeValue(Node node, string value) {
            Assert.IsTrue(node.Type == NodeType.TAG || node.Type == NodeType.NOT_TAG);
            Assert.AreEqual(value, node.Token.Value);
            Assert.AreEqual(0, node.Children.Count);
        }

        private class NodeIter {

            public NodeIter(NodeType type, Token token) {
                Type = type;
                Token = token;
            }

            public NodeIter(Node node) {
                Type = node.Type;
                Token = node.Token;
            }

            public NodeType Type { get; set; }

            public Token Token { get; set; }

        }

    }
}
