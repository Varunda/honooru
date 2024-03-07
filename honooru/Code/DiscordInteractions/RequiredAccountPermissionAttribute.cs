using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Services.Db;
using honooru.Services.Repositories;

namespace honooru.Code.DiscordInteractions {

    /// <summary>
    ///     Attribute to require a user to have an account permission
    /// </summary>
    public class RequiredAccountPermissionAttribute {

        public static async Task<bool> Execute(BaseContext ctx, List<string> permissions) {
            ILogger<RequiredAccountPermissionAttribute> logger = ctx.Services.GetRequiredService<ILogger<RequiredAccountPermissionAttribute>>();
            AppAccountDbStore accountDb = ctx.Services.GetRequiredService<AppAccountDbStore>();

            AppAccount? account = await accountDb.GetByDiscordID(ctx.User.Id, CancellationToken.None);
            if (account == null) {
                logger.LogTrace($"User {ctx.User.Id} does not have an account");
                return false;
            }

            AppAccountPermissionRepository permRepo = ctx.Services.GetRequiredService<AppAccountPermissionRepository>();

            // get at least one AppAccountPermssion the user has

            List<AppGroupPermission> perms = await permRepo.GetByAccountID(account.ID);
            AppGroupPermission? perm = perms.FirstOrDefault(iter => permissions.IndexOf(iter.Permission) > -1);

            if (perm == null) {
                logger.LogDebug($"User {ctx.User.GetDisplay()} lacks any of the following permissions: {string.Join(", ", permissions)}");
                return false;
            }

            logger.LogDebug($"User {ctx.User.GetDisplay()} has permission {perm.ID}/{perm.Permission}");
            return true;
        }

    }

    /// <summary>
    ///     Attribute on slash commands to require an account permission
    /// </summary>
    public class RequiredAccountPermissionSlashAttribute : SlashCheckBaseAttribute {

        public List<string> Permissions { get; }

        public RequiredAccountPermissionSlashAttribute(params string[] perms) {
            Permissions = perms.ToList();
        }

        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) {
            return RequiredAccountPermissionAttribute.Execute(ctx, Permissions);
        }
    }

    /// <summary>
    ///     Attribute on a context menu command to require an account permission
    /// </summary>
    public class RequiredAppPermissionContextAttribute : ContextMenuCheckBaseAttribute {

        public List<string> Permissions { get; }

        public RequiredAppPermissionContextAttribute(params string[] perms) {
            Permissions = perms.ToList();
        }

        public override Task<bool> ExecuteChecksAsync(ContextMenuContext ctx) {
            return RequiredAccountPermissionAttribute.Execute(ctx, Permissions);
        }

    }

}
