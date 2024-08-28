using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace honooru.Models.Search {

    public class Ast : IEnumerable<Node> {

        public Ast(Node root) {
            this.Root = root;
        }

        public Node Root { get; set; }

        /// <summary>
        ///     get a <see cref="IEnumerable{T}"/> that iterates thru the <see cref="Node"/>s starting from the left
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Node> GetEnumerator() {
            List<Node> nodes = _BuildEnumerable(Root, new List<Node>());

            return nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private List<Node> _BuildEnumerable(Node node, List<Node> list) {
            list.Add(node);

            foreach (Node child in node.Children) {
                _BuildEnumerable(child, list);
            }

            return list;
        }

        public string Print(bool indented = false) {
            if (indented) {
                return _PrintNodeIndent(Root, 0);
            } else {
                return _PrintNode(Root);
            }
        }

        private string _PrintNode(Node node, int depth = 0) {
            if (node.Type == NodeType.TAG) {
                return node.Token.Value;
            }

            StringBuilder sb = new();
            sb.Append(node.Type.ToString().ToUpper());
            sb.Append(" ");

            if (node.Children.Count > 0) {
                sb.Append("(");
                sb.Append(string.Join(", ", node.Children.Select(iter => _PrintNode(iter, depth + 1))));
                sb.Append(")");
            } else {
                sb.Append(node.Token.Value);
            }

            return sb.ToString();
        }

        private string _PrintNodeIndent(Node node, int depth = 0) {
            StringBuilder sb = new();
            for (int i = 0; i < depth; ++i) { sb.Append("\t"); }
            sb.Append(node.Type.ToString().ToUpper());
            sb.Append("=");

            if (node.Children.Count > 0) {
                sb.Append("(");
                sb.Append("\n");
                sb.Append(string.Join(" ", node.Children.Select(iter => _PrintNodeIndent(iter, depth + 1))));
                sb.Append("\n");
                for (int i = 0; i < depth; ++i) { sb.Append("\t"); }
                sb.Append(")");
                sb.Append("\n");
            } else {
                sb.Append("'" + node.Token.Value + "'");
                sb.Append("\n");
            }

            return sb.ToString();
        }

        public override int GetHashCode() {
            return Root.GetHashCode();
        }

    }
}
