using System;

namespace honooru.Models {

    /// <summary>
    ///     Represents a grant to let a user perform a protected action
    /// </summary>
    public class AppGroupPermission {

        /// <summary>
        ///     Unique ID of the permission
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     What <see cref="AppAccount"/> this permission is granted to
        /// </summary>
        public ulong GroupID { get; set; }

        /// <summary>
        ///     What the permission is
        /// </summary>
        public string Permission { get; set; } = "";

        /// <summary>
        ///     When this permission was added
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     What <see cref="AppAccount"/> granted this permission
        /// </summary>
        public ulong GrantedByID { get; set; }

    }
}
