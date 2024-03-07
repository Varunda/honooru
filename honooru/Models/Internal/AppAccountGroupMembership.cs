using System;

namespace honooru.Models.Internal {

    public class AppAccountGroupMembership {

        /// <summary>
        ///     unique ID
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        ///     ID of the <see cref="AppAccount"/> this membership is for
        /// </summary>
        public ulong AccountID { get; set; }

        /// <summary>
        ///     ID of the <see cref="AppGroup"/> this membership is for
        /// </summary>
        public ulong GroupID { get; set; }

        /// <summary>
        ///     when this entry was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     who granted the user this permission
        /// </summary>
        public ulong GrantedByAccountID { get; set; }

    }
}
