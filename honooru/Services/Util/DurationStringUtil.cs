using System;

namespace honooru.Services.Util {

    public class DurationStringUtil {

        /// <summary>
        ///     parse a string into a <see cref="TimeSpan"/>.
        ///     for example, "1h40m" would be translated into a <see cref="TimeSpan"/> of 1 hour and 40 minutes.
        ///     d => days, h => hours, m => minutes, s => seconds (case-insensitive)
        /// </summary>
        /// <param name="input">input string</param>
        /// <returns>
        ///     a <see cref="TimeSpan"/> if <paramref name="input"/> was not empty,
        ///     or <c>null</c> if <paramref name="input"/> was empty
        /// </returns>
        /// <exception cref="Exception">if the input contained invalid characters</exception>
        public static TimeSpan Parse(string input) {
            if (string.IsNullOrWhiteSpace(input)) {
                throw new Exception($"empty input");
            }

            TimeSpan span = TimeSpan.Zero;

            string word = "";
            foreach (char iter in input) {
                char i = char.ToLower(iter);
                if (char.IsDigit(i)) {
                    word += i;
                } else if (i == 'd' || i == 'h' || i == 'm' || i == 's') {
                    if (int.TryParse(word, out int value) == false) {
                        throw new Exception($"failed to parse '{word}' to a valid int");
                    }

                    word = "";

                    if (i == 'd') {
                        span += TimeSpan.FromDays(value);
                    } else if (i == 'h') {
                        span += TimeSpan.FromHours(value);
                    } else if (i == 'm') {
                        span += TimeSpan.FromMinutes(value);
                    } else if (i == 's') {
                        span += TimeSpan.FromSeconds(value);
                    } else {
                        throw new Exception($"unchecked letter character '{i}'");
                    }
                } else {
                    throw new Exception($"invalid character {i}");
                }
            }

            return span;
        }

        public static bool TryParse(string input, out TimeSpan span) {
            try {
                span = Parse(input);
                return true;
            } catch {
                span = TimeSpan.Zero;
                return false;
            }
        }


    }
}
