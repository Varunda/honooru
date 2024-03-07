using System.Diagnostics;

namespace honooru.Code.Tracking {

    public static class AppActivitySource {

        public static readonly string ActivitySourceName = "Honooru";

        /// <summary>
        ///     Root activity source timing is done from
        /// </summary>
        public static readonly ActivitySource Root = new ActivitySource(ActivitySourceName);

    }
}
