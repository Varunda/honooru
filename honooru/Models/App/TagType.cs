﻿namespace honooru.Models.App {

    public class TagType {

        /// <summary>
        ///     unique ID
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     name of the tag type
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     hex color, does not include a leading #
        /// </summary>
        public string HexColor { get; set; } = "";

        /// <summary>
        ///     alias of the tag type. for example, if the name is general, the alias of gen: could be used
        /// </summary>
        public string Alias { get; set; } = "";

    }
}
