using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using honooru.Models.Discord;

namespace honooru.Models {

    public class DiscordOptions {

        /// <summary>
        ///     If Discord features are enabled or not
        /// </summary>
        public bool Enabled { get; set; } = false;

        public string Key { get; set; } = "aaa";

        /// <summary>
        ///     Client key
        /// </summary>
        public string ClientId { get; set; } = "";

        /// <summary>
        ///     client secret
        /// </summary>
        public string ClientSecret { get; set; } = "";

        /// <summary>
        ///     Will the global commands be registered globally? Or just in the test server
        /// </summary>
        public bool RegisterGlobalCommands { get; set; } = false;

    }

}
