using DSharpPlus.SlashCommands;
using System.Threading.Tasks;
using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Services;
using honooru.Services.Repositories;
using System.Linq;

namespace honooru.Code.DiscordInteractions {

    /// <summary>
    ///     Class to inherit from if executing a Discord slash command requires a specific account permission
    /// </summary>
    public class PermissionSlashCommand : ApplicationCommandModule {

        public AppCurrentAccount _CurrentUser { internal get; set; } = default!;
        public AppPermissionRepository _PermissionRepository { internal get; set; } = default!;

        /// <summary>
        ///     Check if a Discord user performing a slash command has the correct account permission
        /// </summary>
        /// <param name="ctx">InteractionContext from the method</param>
        /// <param name="permissions">List of permissions that user can have. This is an OR operation</param>
        /// <returns>
        ///     True if the user performing the command in <paramref name="ctx"/>
        ///     has one of the permissions passed in <paramref name="permissions"/>,
        ///     otherwise <c>false</c>, which will also respond to the command with an appropriate message
        /// </returns>
        internal async Task<bool> _CheckPermission(InteractionContext ctx, params string[] permissions) {
            AppAccount? user = await _CurrentUser.GetDiscord(ctx);
            if (user == null) {
                await ctx.CreateImmediateText("You do not have an account");
                return false;
            }

            AppGroupPermission? neededPerm = (await _PermissionRepository.GetByAccountID(user.ID)).FirstOrDefault(iter => permissions.Contains(iter.Permission));
            if (neededPerm == null) {
                await ctx.CreateImmediateText("You lack the correct permission");
                return false;
            }

            return true;
        }

    }
}
