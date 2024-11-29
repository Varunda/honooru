using System;

namespace honooru.Models.App {

    /// <summary>
    ///     represents an API key that can be used to auth against the API
    /// </summary>
    public class ApiKey {

        /// <summary>
        ///     ID of the <see cref="AppAccount"/> this api key is for
        /// </summary>
        public ulong UserID { get; set; }

        /// <summary>
        ///     Secret value used for the auth
        /// </summary>
        public string ClientSecret { get; set; } = "";

        /// <summary>
        ///     when this API key was created
        /// </summary>
        public DateTime Timestamp { get; set; }

    }
}
