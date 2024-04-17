using System;

namespace honooru.Models {

    /// <summary>
    ///     Represents the info about an account with the app
    /// </summary>
    public class AppAccount {

        /// <summary>
        ///     Default ID of the system account
        /// </summary>
        public const ulong SystemID = 1ul;

        /// <summary>
        ///     ID of the account
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     Name the owner of the account is known by
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     Timestamp of when the account was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     Discord ID of the account owner
        /// </summary>
        public string DiscordID { get; set; } = "";

        /// <summary>
        ///     When this account was deleted
        /// </summary>
        public DateTime? DeletedOn { get; set; }

        /// <summary>
        ///     Who deleted this account
        /// </summary>
        public long? DeletedBy { get; set; }

    }
}
