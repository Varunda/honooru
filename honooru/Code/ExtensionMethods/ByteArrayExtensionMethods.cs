using System.Linq;

namespace honooru.Code.ExtensionMethods {

    public static class ByteArrayExtensionMethods {

        public static string JoinToString(this byte[] arr) {
            return string.Join("", arr.Select(iter => iter.ToString("x2")));
        }

    }
}
