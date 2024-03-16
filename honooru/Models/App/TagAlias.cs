namespace honooru.Models.App {

    public class TagAlias {

        /// <summary>
        ///     unique alias that maps to another tag
        /// </summary>
        public string Alias { get; set; } = "";

        /// <summary>
        ///     id of the <see cref="Tag"/> this alias is for
        /// </summary>
        public ulong TagID { get; set; }

    }
}
