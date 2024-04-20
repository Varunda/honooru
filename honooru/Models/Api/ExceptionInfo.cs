using System;

namespace honooru.Models.Api {

    /// <summary>
    ///     a JSON serializable exception
    /// </summary>
    public class ExceptionInfo {

        public ExceptionInfo() { }

        /// <summary>
        ///     ctor to create an <see cref="ExceptionInfo"/> from an <see cref="Exception"/>
        /// </summary>
        /// <param name="ex">extension to create the exception info from</param>
        /// <param name="includeStackTrace">if the stack trace will be included</param>
        /// <param name="includeInnerException">if the inner exception will be included</param>
        public ExceptionInfo(Exception ex, bool includeStackTrace = true, bool includeInnerException = true) {
            Type = ex.GetType().FullName;
            Message = ex.Message;
            Source = ex.Source;
            StackTrace = includeStackTrace ? ex.StackTrace : null;

            if (includeInnerException && ex.InnerException is not null) {
                InnerException = new ExceptionInfo(ex.InnerException, includeInnerException, includeStackTrace);
            }
        }

        public string? Type { get; set; }

        public string Message { get; set; } = "";

        public string? Source { get; set; }

        public string? StackTrace { get; set; }

        public ExceptionInfo? InnerException { get; set; } = null;

    }
}
