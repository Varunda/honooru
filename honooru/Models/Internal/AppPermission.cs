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
        public readonly static AppPermission AppTagEdit = new(APP_TAG_EDIT, "edit tags");

        public const string APP_POST_EDIT = "App.Post.Edit";
        public readonly static AppPermission AppPostEdit = new(APP_POST_EDIT, "edit a post");

        public const string APP_POST_DELETE = "App.Post.Delete";
        public readonly static AppPermission AppPostDelete = new(APP_POST_DELETE, "delete a post");

        public const string APP_POST_ERASE = "App.Post.Erase";
        public readonly static AppPermission AppPostErase = new(APP_POST_ERASE, "erase a post");

        public const string APP_POST_RESTORE = "App.Post.Restore";
        public readonly static AppPermission AppPostRestore = new(APP_POST_RESTORE, "restore a post");

        public const string APP_POOL_CREATE = "App.Pool.Create";
        public readonly static AppPermission AppPoolCreate = new(APP_POOL_CREATE, "create a post pool");

        public const string APP_POOL_DELETE = "App.Pool.Delete";
        public readonly static AppPermission AppPoolDelete = new(APP_POOL_DELETE, "delete a post pool");

        public const string APP_POOL_ENTRY_ADD = "App.PoolEntry.Add";
        public readonly static AppPermission AppPoolEntryAdd = new(APP_POOL_ENTRY_ADD, "add a post to a pool");

        public const string APP_POOL_ENTRY_REMOVE = "App.PoolEntry.Remove";
        public readonly static AppPermission AppPoolEntryRemove = new(APP_POOL_ENTRY_REMOVE, "remove a post from a pool");

    }
}
