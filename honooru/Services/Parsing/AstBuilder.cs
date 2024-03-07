using honooru.Code.Exceptions;
using honooru.Models.Search;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using honooru.Code.ExtensionMethods;

namespace honooru.Services.Parsing {

    public class AstBuilder {

        private readonly ILogger<AstBuilder> _Logger;

        public AstBuilder(ILogger<AstBuilder> logger) {
            _Logger = logger;
        }

        public Ast Build(List<Token> input) {
            Node root = new(NodeType.AND, new Token(TokenType.DEFAULT, ""), null);

            if (input.Count == 0) {
                _Logger.LogDebug($"received no tokens, returning empty AST");
                return new Ast(root);
            }

            _Logger.LogDebug($"generating AST [input count={input.Count}]");

            TokenStream tokens = new(input);
            tokens.MoveNext();

            Token? iter = tokens.Current;
            Stack<Node> stack = new();
            stack.Push(root);

            Node parent = stack.Peek();

            while (iter != null) {

                _Logger.LogTrace($"AST iteration [iter.Type={iter.Type}] [iter.Value={iter.Value}] [parent={parent.Type}] [parent.Children count={parent.Children.Count}]");

                if (iter.Type == TokenType.WORD) {
                    // the parser doesn't use a look-behind, so we have to check in the valid spots for a META token
                    //      to appear for them.
                    // this catches cases like
                    //      user : > 100
                    //      ^ (CURRENT TOKEN)
                    // by checking the next token
                    if (tokens.PeekNext()?.Type == TokenType.META) {
                        if (parent.Type == NodeType.OR) {
                            throw new AstParseException(iter, $"cannot have a META lookup within an OR statement");
                        }

                        _Logger.LogTrace($"look-ahead found META token");
                        Token? meta = tokens.GetNext();
                        if (meta == null) {
                            throw new SystemException($"something is fucked");
                        }

                        Token? opOrValue = tokens.GetNext();
                        if (opOrValue == null) {
                            throw new AstParseException(iter, $"look ahead failed: no token after META token");
                        }

                        Token? op = null;
                        Token? value = null;
                        if (opOrValue.Type == TokenType.OPERATOR) {
                            _Logger.LogTrace($"token is OPERATOR, checking for WORD [type={opOrValue.Type}] [value={opOrValue.Value}]");
                            op = opOrValue;
                            value = tokens.GetNext();
                            if (value == null) { throw new AstParseException(iter, $"look ahead failed: no token after OPERATOR token (for META)"); }
                            if (value.Type != TokenType.WORD) { throw new AstParseException(iter, $"look ahead failed: expected WORD token after OPERATOR token (for META), got {value.Type} instead"); }
                        } else if (opOrValue.Type == TokenType.WORD) {
                            op = new Token(TokenType.OPERATOR, "=");
                            value = opOrValue;
                        } else {
                            throw new AstParseException(iter, $"look ahead failed: expected WORD token after META token, got {opOrValue.Type} instead");
                        }

                        _Logger.LogTrace($"creating META node [field={iter.Value}] [operator='{op.Value}'] [value='{value.Value}']");

                        Node metaParent = _c(NodeType.META, meta, parent);
                        _c(NodeType.META_FIELD, iter, metaParent);
                        _c(NodeType.META_OPERATOR, op, metaParent);
                        _c(NodeType.META_VALUE, value, metaParent);
                    } else {
                        _c(NodeType.TAG, iter, parent);
                    }

                    if (parent.Type == NodeType.OR) {
                        Token? next = tokens.GetNext();
                        if (next == null || next.Type != TokenType.OR_CONTINUE) {
                            throw new AstParseException(iter, $"look ahead failed: expected to see OR_CONTINUE token");
                        }
                    }
                } else if (iter.Type == TokenType.OR_START) {
                    stack.Push(_c(NodeType.OR, iter, parent));
                    parent = stack.Peek();

                    Token? next = tokens.GetNext();
                    if (next == null || next.Type != TokenType.WORD) {
                        throw new AstParseException(iter, $"failed to get WORD token after OR_START");
                    }

                    _c(NodeType.TAG, next, parent);
                } else if (iter.Type == TokenType.OR_CONTINUE) {
                    Token? next = tokens.GetNext();
                    if (next == null || next.Type != TokenType.WORD) {
                        throw new AstParseException(iter, $"failed to get WORD token after OR_CONTINUE");
                    }

                    _c(NodeType.TAG, next, parent);
                } else if (iter.Type == TokenType.OR_END) {
                    parent = stack.Pop(); // get rid of the OR node
                    if (parent.Type != NodeType.OR) {
                        throw new AstParseException(iter, $"expected OR {nameof(Node)} after popping OR_END token, got {parent.Type} instead");
                    }

                    parent = stack.Peek(); // back to whatever node was before the OR
                } else if (iter.Type == TokenType.NOT) {
                    Token? next = tokens.GetNext();
                    if (next == null || next.Type != TokenType.WORD) {
                        throw new AstParseException(iter, $"failed to get WORD token after NOT");
                    }

                    _c(NodeType.NOT_TAG, next, parent);
                } else if (iter.Type == TokenType.META) {
                    throw new AstParseException(iter, $"unexpected place for a META tag, was the previous token a WORD token?");
                } else if (iter.Type == TokenType.END) {
                    break;
                } else {
                    throw new NotImplementedException($"haven't implemented {iter.Type} {nameof(Node)}s yet");
                }

                iter = tokens.GetNext();
            }

            if (tokens.MoveNext() == true) {
                throw new AstParseException(null, $"invalid token list: expected MoveNext to fail after consuming all tokens, but have {tokens.Current}");
            }

            if (stack.Count > 1) {
                throw new AstParseException(null, $"invalid token list: expected one {nameof(Node)} in stack after consuming all {nameof(Token)}s, had {stack.Count} instead");
            }

            Node? treeRoot = stack.PopOrDefault();
            if (treeRoot == null) {
                throw new AstParseException(null, $"failed to get root tree node, stack had no nodes left on it (did you double pop somewhere?)");
            }

            // if the root node only has a single child, we can just use that node instead,
            //      for example:
            //          INPUT:
            //              AND ( OR ( hi howdy ) )
            //          OUTPUT:
            //              hi OR howdy AND ""
            //      which doesn't mean much really
            // instead, we can just use the OR instead
            while (treeRoot.Children.Count == 1) {
                treeRoot = treeRoot.Children[0];
            }

            // it's fine for like a NOT node to have 0 children, as it's the only node
            if ((treeRoot.Type == NodeType.AND || treeRoot.Type == NodeType.OR) && treeRoot.Children.Count == 0) {
                throw new AstParseException(null, $"invalid token list: expected root {nameof(Node)} to have >0 children, had {treeRoot.Children.Count} instead");
            }

            return new Ast(treeRoot);
        }

        /// <summary>
        ///     helper function to create a Node, and add it to the <see cref="Node.Children"/> of <paramref name="parent"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="token"></param>
        /// <param name="parent"></param>
        /// <returns>
        ///     the newly created node
        /// </returns>
        private Node _c(NodeType type, Token token, Node parent) => new Node(type, token, parent);

    }
}
