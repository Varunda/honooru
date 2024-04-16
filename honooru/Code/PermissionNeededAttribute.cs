using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Services.Db;
using honooru.Services.Repositories;
using honooru.Services;

namespace honooru.Code {

    /// <summary>
    ///     Attribute to add to actions to require a user to have a <see cref="AppAccount"/>,
    ///     and that account has the necessary permissions
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class PermissionNeededAttribute : TypeFilterAttribute {

        public PermissionNeededAttribute(params string[] perms) : base(typeof(PermissionNeededFilter)) {
            Arguments = new object[] { perms };
        }

    }

    public class PermissionNeededFilter : IAsyncAuthorizationFilter {

        private readonly ILogger<PermissionNeededFilter> _Logger;
        private readonly IHttpContextAccessor _Context;
        private readonly AppAccountDbStore _AppAccountDb;
        private readonly AppPermissionRepository _PermissionRepository;
        private readonly AppCurrentAccount _CurrentAccount;

        public readonly List<string> Permissions;

        public PermissionNeededFilter(ILogger<PermissionNeededFilter> logger,
            IHttpContextAccessor context, AppAccountDbStore appAccountDb,
            AppPermissionRepository permissionRepository, AppCurrentAccount currentAccount,
            string[] perms) { 

            Permissions = perms.ToList();

            _Logger = logger;
            _Context = context;
            _AppAccountDb = appAccountDb;
            _PermissionRepository = permissionRepository;
            _CurrentAccount = currentAccount;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
            HttpContext httpContext = context.HttpContext;
            if (httpContext == null) {
                throw new ArgumentNullException($"_Context.HttpContext cannot be null");
            }

            AppAccount? account = await _CurrentAccount.Get();
            if (account == null) {
                _Logger.LogTrace($"user was authed, but does not have an account [url={context.HttpContext.Request.Path.Value}]");

                if (context.HttpContext.Response.HasStarted == true) {
                    _Logger.LogError($"response started, cannot set 403Forbidden");
                    throw new ApplicationException($"cannot forbid access to action, response has started");
                }

                if (httpContext.Request.Path.StartsWithSegments("/api") == true) {
                    context.Result = new ApiResponse(403, new Dictionary<string, string>() {
                        { "error", "user does not have an account" }
                    });
                } else {
                    context.Result = new RedirectToActionResult("Unauthorized", "Home", null);
                }

                return;
            }

            _Logger.LogTrace($"checking if user has permission [account={account.ID}/{account.Name}] [Permissions={string.Join(", ", Permissions)}] " 
                + $"[url={context.HttpContext.Request.Path.Value}]");

            // user is owner
            if (account.ID == 2) {
                _Logger.LogTrace($"user has permission as they are the owner [account={account.Name}]");
                return;
            }

            HashSet<string> accountPerms = new();

            List<AppGroupPermission> perms = await _PermissionRepository.GetByAccountID(account.ID);
            foreach (AppGroupPermission perm in perms) {
                accountPerms.Add(perm.Permission.ToLower());
            }

            bool hasPerm = false;
            foreach (string perm in Permissions) {
                if (accountPerms.Contains(perm.ToLower())) {
                    hasPerm = true;
                    break;
                }
            }

            if (hasPerm == false) {
                if (context.HttpContext.Response.HasStarted == true) {
                    _Logger.LogError($"Response started, cannot set 403Forbidden");
                    throw new ApplicationException($"Cannot forbid access to psb admin action, response has started.");
                }

                if (httpContext.Request.Path.StartsWithSegments("/api") == true) {
                    context.Result = new ApiResponse(403, new Dictionary<string, string>() {
                        { "error", "no permission" }
                    });
                } else {
                    context.Result = new RedirectToActionResult("Index", "Unauthorized", null);
                }
            }

        }

    }

}
