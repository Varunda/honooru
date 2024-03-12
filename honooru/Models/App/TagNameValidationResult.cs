namespace honooru.Models.App {

    public class TagNameValidationResult {

        /// <summary>
        ///     input name for validation
        /// </summary>
        public string Input { get; set; } = "";

        /// <summary>
        ///     if <see cref="Valid"/> is <c>false</c>, why <see cref="Input"/> is not valid
        /// </summary>
        public string Reason { get; set; } = "";

        /// <summary>
        ///     if <see cref="Input"/> is a valid tag name or not
        /// </summary>
        public bool Valid { get; set; } = false;

    }
}
