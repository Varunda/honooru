using honooru.Models.App;
using Microsoft.Extensions.Logging;

namespace honooru.Services.Util {

    public class TagValidationService {

        private readonly ILogger<TagValidationService> _Logger;

        public TagValidationService(ILogger<TagValidationService> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     perform a validation on a name
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public TagNameValidationResult ValidateTagName(string tagName) {
            tagName = tagName.ToLower();

            TagNameValidationResult res = new();
            res.Input = tagName;

            if (string.IsNullOrWhiteSpace(tagName)) {
                res.Valid = false;
                res.Reason = "name cannot be empty or only whitespace";
                return res;
            }

            for (int i = 0; i < tagName.Length; ++i) {
                char c = tagName[i];

                if (i == 0 && c == '-') {
                    res.Valid = false;
                    res.Reason = $"cannot have a dash as the first character";
                    return res;
                }

                // don't allow empty paren tags such as name_()
                if (c == '(' && (i + 1 < tagName.Length) && (tagName[i + 1] == ')')) {
                    res.Valid = false;
                    res.Reason = $"cannot have an empty string within a pair of parenthesis (at index {i})";
                    return res;
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
                    || c == '-'
                    || c == '/'
                    ) {
                    continue;
                }

                res.Valid = false;
                res.Reason = $"invalid character '{c}'";
                return res;
            }

            res.Valid = true;
            return res;
        }

    }
}
