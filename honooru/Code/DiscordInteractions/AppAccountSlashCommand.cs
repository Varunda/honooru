﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Repositories;

namespace honooru.Code.DiscordInteractions {

    public class AppAccountSlashCommand : PermissionSlashCommand {

        public ILogger<AppAccountSlashCommand> _Logger { set; private get; } = default!;
        public AppAccountDbStore _AccountDb { set; private get; } = default!;

        /// <summary>
        ///     Slash command to get the current <see cref="AppAccount"/>
        /// </summary>
        /// <param name="ctx">Provided context</param>
        [SlashCommand("whoami", "See what permissions you have in a command")]
        public async Task WhoAmICommand(InteractionContext ctx) {
            AppAccount? account = await _CurrentUser.GetDiscord(ctx);
            if (account == null) {
                await ctx.CreateImmediateText($"You do not have an account", true);
                return;
            }

            await ctx.CreateDeferred(true);

            List<AppGroupPermission> perms = await _PermissionRepository.GetByAccountID(account.ID);
            string s = $"Permissions on this account ({perms.Count}): \n";
            s += string.Join("\n", perms.Select(iter => $"`{iter.Permission}`"));

            await ctx.EditResponseText($"Account ID: {account.ID}\n{s}");
        }

        /// <summary>
        ///     Slash command to get the permission of a target user
        /// </summary>
        /// <param name="ctx">Provided context</param>
        /// <param name="user">Discord user to get the permissions of</param>
        [SlashCommand("whois", "See what permissions another account has")]
        public async Task WhoIsCommand(InteractionContext ctx,
            [Option("user", "What user to target with this command")] DiscordUser user) {

            await ctx.CreateDeferred(true);

            AppAccount? targetAccount = await _AccountDb.GetByDiscordID(user.Id, CancellationToken.None);
            if (targetAccount == null) {
                await ctx.EditResponseText($"Target user does not have an account");
                return;
            }

            List<AppGroupPermission> perms = await _PermissionRepository.GetByAccountID(targetAccount.ID);
            string s = $"Permissions on this account ({perms.Count}): \n";
            s += string.Join("\n", perms.Select(iter => $"`{iter.Permission}`"));

            await ctx.EditResponseText($"Account ID: {targetAccount.ID}\n{s}");
        }

        /// <summary>
        ///     Context menu command to get the account permissions of the target user
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [ContextMenu(ApplicationCommandType.UserContextMenu, "Account whois")]
        public async Task WhoIsContext(ContextMenuContext ctx) {
            DiscordMember source = ctx.Member;
            DiscordMember target = ctx.TargetMember;

            await ctx.CreateDeferred(true);

            AppAccount? targetAccount = await _AccountDb.GetByDiscordID(target.Id, CancellationToken.None);
            if (targetAccount == null) {
                await ctx.EditResponseText($"Target user does not have an account");
                return;
            }

            List<AppGroupPermission> perms = await _PermissionRepository.GetByAccountID(targetAccount.ID);
            string s = $"Permissions on this account ({perms.Count}): \n";
            s += string.Join("\n", perms.Select(iter => $"`{iter.Permission}`"));

            await ctx.EditResponseText($"Account ID: {targetAccount.ID}\n{s}");
        }

    }
}
