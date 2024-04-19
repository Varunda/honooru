using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace honooru.Models.Search {

    public class Node {

        public Node(NodeType type, Token token, Node? parent) {
            if (Parent != null && Parent == this) {
                throw new InvalidOperationException($"a {nameof(Node)} cannot have its {nameof(Node.Parent)} be itself");
            }

            this.Type = type;
            this.Token = token;
            this.Parent = parent;
            this.Children = new List<Node>();

            if (Parent != null) {
                Parent.Children.Add(this);
            }
        }

        [JsonIgnore]
        public Node? Parent { get; set;  }

        public NodeType Type { get; set; }

        public Token Token { get; set; }

        public List<Node> Children { get; set; } = new();

        private int? _Depth = null;
        public int Depth {
            get {
                if (_Depth != null) {
                    return _Depth.Value;
                }

                Node? iter = Parent;

                int i = 0;
                while (iter != null) {
                    ++i;
                    iter = iter.Parent;
                }

                _Depth = i;

                return _Depth.Value;
            }
        }

        public override string ToString() {
            return $"<{nameof(Node)} [{nameof(Type)}={Type}] [Token={Token}] [Children.count={Children.Count}] [has parent={Parent != null}]>";
        }

        public override int GetHashCode() {
            int hash = HashCode.Combine((int)Type, Token);

            foreach (Node node in Children) {
                hash ^= node.GetHashCode();
            }

            return hash;
        }

    }

    public enum NodeType {

        TAG,

        AND,

        OR,

        NOT,

        META,

        META_FIELD,

        META_OPERATOR,

        META_VALUE

    }

    public static class NodeExtensionMethods {

        /// <summary>
        ///     get the root <see cref="Node"/>, which is defined as the <see cref="Node"/> 
        ///     with no <see cref="Node.Parent"/>
        /// </summary>
        /// <param name="node">extension instance</param>
        /// <returns>
        ///     the first <see cref="Node"/> with a null <see cref="Node.Parent"/>,
        ///     iterating upwards if <see cref="Node.Parent"/> is not null
        /// </returns>
        public static Node GetRoot(this Node node) {
            Node iter = node;

            while (true) {
                if (iter.Parent == null) {
                    return iter;
                }

                iter = iter.Parent;
            }
        }

    }

}
