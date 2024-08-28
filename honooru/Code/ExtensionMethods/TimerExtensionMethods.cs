using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace honooru.Code.ExtensionMethods {

    public static class TimerExtensionMethods {

        /// <summary>
        ///     get how many milliseconds have passed on the timer,
        ///     and call <see cref="Stopwatch.Restart"/>
        /// </summary>
        /// <param name="timer">static instance</param>
        /// <returns>
        ///     the value of <see cref="Stopwatch.ElapsedMilliseconds"/>
        /// </returns>
        public static long ElapsedMillisecondsReset(this Stopwatch timer) {
            long ms = timer.ElapsedMilliseconds;
            timer.Restart();
            return ms;
        }

    }
}
