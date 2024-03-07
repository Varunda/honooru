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

        public string Print() {
            return _PrintNode(Root);
        }

        private string _PrintNode(Node node) {
            if (node.Type == NodeType.TAG) {
                return node.Token.Value;
            }

            if (node.Type == NodeType.NOT_TAG) {
                return $"NOT ({node.Token.Value})";
            }

            StringBuilder sb = new();
            sb.Append(node.Type.ToString().ToUpper());
            sb.Append(' ');

            if (node.Children.Count > 0) {
                sb.Append("(");
                sb.Append(string.Join(", ", node.Children.Select(iter => _PrintNode(iter))));
                sb.Append(")");
            }

            return sb.ToString();
        }

    }
}
