using System.Collections.Generic;

namespace honooru.Models.Internal {

    /// <summary>
    ///     Permission that can be granted to a <see cref="AppAccount"/>
    /// </summary>
    public class AppPermission {

        /// <summary>
        ///     List of all <see cref="AppPermission"/>s that exist
        /// </summary>
        public readonly static List<AppPermission> All = new();

        /// <summary>
        ///     Unique ID of the permission
        /// </summary>
        public string ID { get; }

        /// <summary>
        ///     What this permission grants
        /// </summary>
        public string Description { get; }

        public AppPermission(string ID, string desc) {
            this.ID = ID;
            this.Description = desc;

            AppPermission.All.Add(this);
        }

        public const string APP_ACCOUNT_ADMIN = "App.Account.Admin";
        public readonly static AppPermission AppAccountAdmin = new(APP_ACCOUNT_ADMIN, "Manage all accounts");

        public const string APP_DISCORD_ADMIN = "App.Discord.Admin";
        public readonly static AppPermission AppDiscordAdmin = new(APP_DISCORD_ADMIN, "Manage the Discord bot");

        public const string APP_ACCOUNT_GETALL = "App.Account.GetAll";
        public readonly static AppPermission AppAccountGetAll = new(APP_ACCOUNT_GETALL, "Get all accounts");

        public const string APP_VIEW = "App.View";
        public readonly static AppPermission AppView = new(APP_VIEW, "view Honooru");

        public const string APP_UPLOAD = "App.Upload";
        public readonly static AppPermission AppUpload = new(APP_UPLOAD, "upload posts");

        public const string APP_TAG_EDIT = "App.Tag.Edit";

    }
}
