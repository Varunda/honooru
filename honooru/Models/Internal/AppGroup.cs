using System.Collections.Generic;

namespace honooru.Models.Internal {

    public class AppGroup {

        public static readonly ulong Admin = 1;

        /// <summary>
        ///     ID of the group
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     name of the group
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     hex color without the leading #
        /// </summary>
        public string HexColor { get; set; } = "";

        /// <summary>
        ///     a list of <see cref="AppGroup.ID"/>s that this group implies
        /// </summary>
        public List<ulong> Implies { get; set; } = new();

    }
}
