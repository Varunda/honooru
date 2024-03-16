namespace honooru.Models.App {

    /// <summary>
    ///     an implication between two tags. when a post has the <see cref="Tag"/> with <see cref="Tag.ID"/> of <see cref="TagImplication.TagA"/>,
    ///     then it will also have the <see cref="Tag"/> with <see cref="Tag.ID"/> of <see cref="TagB"/>
    /// </summary>
    public class TagImplication {

        public ulong TagA { get; set; }

        public ulong TagB { get; set; }

    }
}
