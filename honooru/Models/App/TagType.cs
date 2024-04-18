namespace honooru.Models.App {

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
        ///     will the frontend use dark or light text when showing this tag type name?
        /// </summary>
        public bool DarkText { get; set; }

        /// <summary>
        ///     alias of the tag type. for example, if the name is general, the alias of gen: could be used
        /// </summary>
        public string Alias { get; set; } = "";

        /// <summary>
        ///     order to display tags within this type. smaller numbers mean highest in order (0 is first)
        /// </summary>
        public short Order { get; set; }

    }
}
