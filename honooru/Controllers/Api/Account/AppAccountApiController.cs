using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using honooru.Code;
using honooru.Models;
using honooru.Models.Internal;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Repositories;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/account")]
    public class AppAccountApiController : ApiControllerBase {

        private readonly ILogger<AppAccountApiController> _Logger;
        private readonly AppCurrentAccount _CurrentUser;

        private readonly AppAccountDbStore _AccountDb;
        private readonly AppAccountPermissionRepository _PermissionRepository;

        public AppAccountApiController(ILogger<AppAccountApiController> logger, AppCurrentAccount currentUser,
            AppAccountPermissionRepository permissionRepository, AppAccountDbStore accountDb) {

            _Logger = logger;
            _CurrentUser = currentUser;

            _PermissionRepository = permissionRepository;
            _AccountDb = accountDb;
        }

        /// <summary>
        ///     Get the current user who is making the API call
        /// </summary>
        /// <response code="200">
        ///     The response will contain the <see cref="AppAccount"/> of the user who made the API call
        /// </response>
        /// <response code="204">
        ///     The user making the API call is either not signed in, or no has no account
        /// </response>
        [HttpGet("whoami")]
        public async Task<ApiResponse<AppAccount>> WhoAmI() {
            AppAccount? currentUser = await _CurrentUser.Get();

            if (currentUser == null) {
                return ApiNoContent<AppAccount>();
            }

            return ApiOk(currentUser);
        }

        /// <summary>
        ///     Get all accounts
        /// </summary>
        /// <response code="200">
        ///     A list of all <see cref="AppAccount"/>s
        /// </response>
        [HttpGet]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        [Authorize]
        public async Task<ApiResponse<List<AppAccount>>> GetAll() {
            List<AppAccount> accounts = await _AccountDb.GetAll(CancellationToken.None);

            return ApiOk(accounts);
        }

        /// <summary>
        ///     Create a new account
        /// </summary>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <param name="discordID"></param>
        /// <response code="200">
        ///     The <see cref="AppAccount.ID"/> of the <see cref="AppAccount"/> that was created using the parameters passed
        /// </response>
        /// <response code="400">
        ///     One of the following validation errors occured:
        ///     <ul>
        ///         <li><paramref name="name"/> was empty or whitespace</li>
        ///         <li><paramref name="email"/> was empty or whitespace</li>
        ///         <li><paramref name="discordID"/> was 0</li>
        ///         <li><paramref name="email"/> is already in use</li>
        ///     </ul>
        /// </response>
        [HttpPost("create")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        [Authorize]
        public async Task<ApiResponse<long>> CreateAccount([FromQuery] string name, [FromQuery] string email, [FromQuery] ulong discordID) {
            List<string> errors = new();

            if (string.IsNullOrWhiteSpace(name)) { errors.Add($"Missing {nameof(name)}"); }
            if (string.IsNullOrWhiteSpace(email)) { errors.Add($"Missing {nameof(email)}"); }

            if (discordID == 0) { errors.Add($"Missing {nameof(discordID)}"); }

            if (errors.Count > 0) {
                return ApiBadRequest<long>($"Validation errors: {string.Join("\n", errors)}");
            }

            AppAccount? existingAccount = await _AccountDb.GetByEmail(email, CancellationToken.None);
            if (existingAccount != null) {
                return ApiBadRequest<long>($"Account for email {email} already exists");
            }

            AppAccount acc = new();
            acc.Name = name;
            acc.Email = email;
            acc.DiscordID = discordID;
            acc.Timestamp = DateTime.UtcNow;

            long ID = await _AccountDb.Insert(acc, CancellationToken.None);

            return ApiOk(ID);
        }

        /// <summary>
        ///     mark an account as inactive
        /// </summary>
        /// <param name="accountID">ID of the account to mark as inactive</param>
        /// <response code="200">
        ///     the account was successfully marked as inactive
        /// </response>
        /// <response code="400">
        ///     <paramref name="accountID"/> was 1, which is the system account, which is not allowed
        /// </response>
        [HttpDelete("{accountID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        [Authorize]
        public async Task<ApiResponse> DeactiviateAccount(long accountID) {
            if (accountID == 1) {
                return ApiBadRequest($"Cannot deactivate account ID 1, which is the system account");
            }

            AppAccount? currentUser = await _CurrentUser.Get();
            if (currentUser == null) {
                return ApiInternalError(new Exception($"current account is null?"));
            }

            await _AccountDb.Delete(accountID, currentUser.ID, CancellationToken.None);

            return ApiOk();
        }


    }
}
