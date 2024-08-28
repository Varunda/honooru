using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Services.Db;
using honooru.Services.Repositories;

namespace honooru.Services {

    /// <summary>
    ///     Service to get the current user making an HTTP request
    /// </summary>
    public class AppCurrentAccount {

        private readonly ILogger<AppCurrentAccount> _Logger;
        private readonly IHttpContextAccessor _Context;
        private readonly AppAccountRepository _AppAccountRepository;

        public AppCurrentAccount(ILogger<AppCurrentAccount> logger,
            IHttpContextAccessor context, AppAccountRepository appAccountRepository) {

            _Logger = logger;
            _Context = context;
            _AppAccountRepository = appAccountRepository;
        }

        /// <summary>
        ///     Get the current user based who a <see cref="BaseContext"/>
        /// </summary>
        /// <param name="ctx">Context of the application command</param>
        /// <returns>
        ///     Null if the field <see cref="BaseContext.Member"/> is null, or null if the user doesn't have an account
        /// </returns>
        public async Task<AppAccount?> GetDiscord(BaseContext ctx) {
            DiscordMember? caller = ctx.Member;
            if (caller == null) {
                return null;
            }

            AppAccount? account = await _AppAccountRepository.GetByDiscordID(caller.Id);

            return account;
        }

        public async Task<AppAccount?> GetByDiscordID(ulong accountID) {
            AppAccount? account = await _AppAccountRepository.GetByDiscordID(accountID);

            return account;
        }

        /// <summary>
        ///     Get the current user, null if the user is not signed in
        /// </summary>
        /// <returns></returns>
        public async Task<AppAccount?> Get() {
            if (_Context.HttpContext == null) {
                _Logger.LogWarning($"_Context.HttpContext is null, cannot get claims");
                return null;
            }

            HttpContext httpContext = _Context.HttpContext;

            if (httpContext.User.Identity == null) {
                _Logger.LogWarning($"httpContext.User.Identity is null");
                return null;
            }

            if (httpContext.User.Identity.IsAuthenticated == false) {
                _Logger.LogWarning($"User is not authed, return them to the sign in");
                return null;
            } else if (httpContext.User is ClaimsPrincipal claims) {
                /*
                string s = "";
                foreach (Claim claim in claims.Claims) {
                    s += $"{claim.Type} = {claim.Value};";
                }
                _Logger.LogDebug($"{s}");
                */

                // Get the email claim of the authed user
                Claim? idClaim = claims.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim == null || string.IsNullOrEmpty(idClaim.Value)) {
                    return null;
                }

                string id = idClaim.Value;

                if (ulong.TryParse(id, out ulong discordID) == false) {
                    throw new InvalidCastException($"failed to convert {id} to a valid ulong");
                }

                return await _AppAccountRepository.GetByDiscordID(discordID, CancellationToken.None);
            } else {
                _Logger.LogWarning($"Unchecked stat of httpContext.User");
            }

            return null;
        }

        public async Task<AppAccount> GetRequired() {
            if (_Context.HttpContext == null) {
                _Logger.LogWarning($"_Context.HttpContext is null, cannot get claims");
                throw new Exception($"user is required (no http context)");
            }

            HttpContext httpContext = _Context.HttpContext;

            if (httpContext.User.Identity == null) {
                _Logger.LogWarning($"httpContext.User.Identity is null");
                throw new Exception($"user is required (identity is null)");
            }

            if (httpContext.User.Identity.IsAuthenticated == false) {
                _Logger.LogWarning($"User is not authed, return them to the sign in");
                throw new Exception($"user is required (user is not authed)");
            } else if (httpContext.User is ClaimsPrincipal claims) {
                /*
                string s = "";
                foreach (Claim claim in claims.Claims) {
                    s += $"{claim.Type} = {claim.Value};";
                }
                _Logger.LogDebug($"{s}");
                */

                // Get the email claim of the authed user
                Claim? idClaim = claims.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim == null || string.IsNullOrEmpty(idClaim.Value)) {
                    throw new Exception($"user is required (missing ID claim)");
                }

                string id = idClaim.Value;

                if (ulong.TryParse(id, out ulong discordID) == false) {
                    throw new InvalidCastException($"failed to convert {id} to a valid ulong");
                }

                return await _AppAccountRepository.GetByDiscordID(discordID, CancellationToken.None)
                    ?? throw new Exception($"user is required (no account from ID)");
            } else {
                _Logger.LogWarning($"Unchecked stat of httpContext.User");
            }

            throw new Exception($"user is required");
        }

    }
}
