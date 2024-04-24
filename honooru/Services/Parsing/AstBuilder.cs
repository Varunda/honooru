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

                if (tokens.PeekNext()?.Type == TokenType.META) {
                    // already adds it to the parent
                    _MakeMetaNode(iter, parent, tokens);
                    iter = tokens.GetNext();
                    continue;
                }

                if (iter.Type == TokenType.WORD) {
                    _c(NodeType.TAG, iter, parent);

                    if (parent.Type == NodeType.OR) {
                        Token next = tokens.GetNext() ?? throw new AstParseException(iter, $"look ahead failed: expected to see OR_CONTINUE token, had null instead");
                        _Logger.LogTrace($"WORD is part of an OR [next={next}]");
                        if (next.Type != TokenType.OR_CONTINUE) {
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

                    if (tokens.PeekNext()?.Type == TokenType.META) {
                        _MakeMetaNode(next, parent, tokens);
                        _Logger.LogTrace($"created META token within OR_START [next={tokens.PeekNext()}]");
                    } else {
                        _Logger.LogTrace($"consuming TAG token after OR_START [token={next}]");
                        _c(NodeType.TAG, next, parent);
                    }

                    // ensure the next tokens are valid options
                    if (tokens.PeekNext()?.Type != TokenType.OR_CONTINUE && tokens.PeekNext()?.Type != TokenType.OR_END) {
                        throw new AstParseException(iter, $"failed to get OR_CONTINUE or an OR_END token after reading a META tag");
                    }
                } else if (iter.Type == TokenType.OR_CONTINUE) {
                    Token? next = tokens.GetNext();
                    if (next == null || next.Type != TokenType.WORD) {
                        throw new AstParseException(iter, $"failed to get WORD token after OR_CONTINUE");
                    }

                    // eat the next token. if it's a META token, eat all the tokens needed for that
                    if (tokens.PeekNext()?.Type == TokenType.META) {
                        _MakeMetaNode(next, parent, tokens);
                        _Logger.LogTrace($"created META token within OR_CONTINUE [next={tokens.PeekNext()}]");
                    } else {
                        _Logger.LogTrace($"consuming TAG token after OR_START [token={next}]");
                        _c(NodeType.TAG, next, parent);
                    }

                    // ensure the next tokens are valid options
                    if (tokens.PeekNext()?.Type != TokenType.OR_CONTINUE && tokens.PeekNext()?.Type != TokenType.OR_END) {
                        throw new AstParseException(iter, $"failed to get OR_CONTINUE or an OR_END token after reading a META tag");
                    }
                } else if (iter.Type == TokenType.OR_END) {
                    parent = stack.Pop(); // get rid of the OR node
                    if (parent.Type != NodeType.OR) {
                        throw new AstParseException(iter, $"expected OR {nameof(Node)} after popping OR_END token, got {parent.Type} instead");
                    }

                    parent = stack.Peek(); // back to whatever node was before the OR
                } else if (iter.Type == TokenType.NOT) {
                    Token next = tokens.GetNext() ?? throw new AstParseException(iter, $"look ahead failed: missing token after NOT");
                    if (next.Type != TokenType.WORD) {
                        throw new AstParseException(iter, $"look ahead failed: needed WORD token, got {next.Type} instead");
                    }

                    _Logger.LogTrace($"found NOT token [next={next}] [parent={parent}]");

                    _c(NodeType.NOT, next, parent);
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

            /*
            // if the root node only has a single child, we can just use that node instead,
            //      for example:
            //          INPUT:
            //              AND ( OR ( hi howdy ) )
            //          OUTPUT:
            //              hi OR howdy AND ""
            //      which doesn't mean much really
            // instead, we can just use the OR instead
            while (treeRoot.Type != NodeType.NOT && treeRoot.Children.Count == 1) {
                treeRoot = treeRoot.Children[0];
                // remove the parent, as the root node in the tree has moved down, so it's parent can DIE
                // (and by die i mean politely garbage collected)
                treeRoot.Parent = null;
            }
            */

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

        private Node _MakeMetaNode(Token iter, Node parent, TokenStream tokens) {
            _Logger.LogTrace($"look-ahead found META token");
            Token meta = tokens.GetNext() ?? throw new SystemException("something is fucked");

            // an operator can optionally be set, but defaults to =
            // for example, width:>10 has the tokens as "width", ">", "10"
            Token? opOrValue = tokens.GetNext();
            if (opOrValue == null) {
                throw new AstParseException(iter, $"look ahead failed: no token after META token");
            }

            Token? op = null;
            Token? value = null;
            if (opOrValue.Type == TokenType.OPERATOR) { // a colon
                _Logger.LogTrace($"token is OPERATOR, checking for WORD [type={opOrValue.Type}] [value={opOrValue.Value}]");
                op = opOrValue;
                value = tokens.GetNext();
                if (value == null) { // expect a token after the colon (prevents something like width:>)
                    throw new AstParseException(iter, $"look ahead failed: no token after OPERATOR token (for META)");
                }
                if (value.Type != TokenType.WORD) {
                    throw new AstParseException(iter, $"look ahead failed: expected WORD token after OPERATOR token (for META), got {value.Type} instead");
                }
            } else if (opOrValue.Type == TokenType.WORD) {
                // assume if no operator is given, that the = operator is used
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

            return metaParent;
        }

    }
}
