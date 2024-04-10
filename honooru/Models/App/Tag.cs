using System;
using System.Collections.Generic;
using System.Linq;

namespace honooru.Models.App {

    public class Tag {

        public static bool Validate(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return false;
            }

            name = name.ToLower();

            for (int i = 0; i < name.Length; ++i) {
                char c = name[i];

                // don't allow empty paren tags such as name_()
                if (c == '(' && (i + 1 < name.Length) && (name[i + 1] == ')')) {
                    return false;
                }

                // only valid characters in a tag
                if (
                    (c == '(' || c == ')')
                    || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9')
                    || c == '_'
                    || c == '\''
                    || c == '.'
                    || c == '!'
                    ) {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        ///     unique ID
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     name of this tag. must be unique
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     ID of the <see cref="TagType"/> this tag is for
        /// </summary>
        public ulong TypeID { get; set; }

        /// <summary>
        ///     when this <see cref="Tag"/> was created
        /// </summary>
        public DateTime Timestamp { get; set; }

    }
}
