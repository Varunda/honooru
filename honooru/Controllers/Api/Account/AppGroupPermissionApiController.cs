﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
    [Route("/api/group-permission")]
    public class AppGroupPermissionApiController : ApiControllerBase {

        private readonly ILogger<AppGroupPermissionApiController> _Logger;
        private readonly AppCurrentAccount _CurrentAccount;

        private readonly AppAccountDbStore _AccountDb;
        private readonly AppGroupDbStore _GroupDb;
        private readonly AppPermissionRepository _PermissionRepository;

        public AppGroupPermissionApiController(ILogger<AppGroupPermissionApiController> logger,
            AppAccountDbStore accountDb, AppPermissionRepository permissionRepository,
            AppCurrentAccount currentAccount, AppGroupDbStore groupDb) {

            _Logger = logger;
            _CurrentAccount = currentAccount;

            _AccountDb = accountDb;
            _PermissionRepository = permissionRepository;
            _GroupDb = groupDb;
        }


        [HttpGet("account/{accountID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse<List<AppGroupPermission>>> GetByAccountID(ulong accountID) {
            List<AppGroupPermission> perms = await _PermissionRepository.GetByAccountID(accountID);

            return ApiOk(perms);
        }

        /// <summary>
        ///     Get the permissions an account has
        /// </summary>
        /// <param name="groupID">ID of the group</param>
        /// <response code="200">
        ///     The response will contain a list of permissions an account has
        /// </response>
        /// <response code="404">
        ///     No <see cref="AppAccount"/> with <see cref="AppAccount.ID"/> of <paramref name="groupID"/> exists
        /// </response>
        [HttpGet("{groupID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        [Authorize]
        public async Task<ApiResponse<List<AppGroupPermission>>> GetByGroupID(ulong groupID) {
            AppGroup? group = await _GroupDb.GetByID(groupID);
            if (group == null) {
                return ApiNotFound<List<AppGroupPermission>>($"{nameof(AppGroup)} {group}");
            }

            List<AppGroupPermission> perms = await _PermissionRepository.GetByGroupID(groupID);

            return ApiOk(perms);
        }

        /// <summary>
        ///     Remove a permission from an account
        /// </summary>
        /// <param name="accPermID">ID of the <see cref="AppGroupPermission"/> to remove</param>
        /// <response code="200">
        ///     The <see cref="AppGroupPermission"/> with <see cref="AppGroupPermission.ID"/>
        ///     of <paramref name="accPermID"/> was successfully deleted
        /// </response>
        /// <response code="404">
        ///     No <see cref="AppGroupPermission"/> with <see cref="AppGroupPermission.ID"/>
        ///     of <paramref name="accPermID"/> exists
        /// </response>
        [HttpDelete("{accPermID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        [Authorize]
        public async Task<ApiResponse> RemoveByID(long accPermID) {
            AppGroupPermission? perm = await _PermissionRepository.GetByID(accPermID);
            if (perm == null) {
                return ApiNotFound($"{nameof(AppGroupPermission)} {accPermID}");
            }

            await _PermissionRepository.DeleteByID(accPermID);

            return ApiOk();
        }

        /// <summary>
        ///     Insert a new permission for a group
        /// </summary>
        /// <param name="groupID">ID of the group to add the permission to</param>
        /// <param name="permission">Permission to be added to the account</param>
        /// <response code="200">
        ///     The reponse will contain the ID of the <see cref="AppGroupPermission"/> that was just created
        ///     using the parameters passed
        /// </response>
        /// <response code="400">
        ///     The account already has a <see cref="AppGroupPermission"/> for <paramref name="permission"/>
        /// </response>
        /// <response code="404">
        ///     One of the following objects count not be found:
        ///     <ul>
        ///         <li><see cref="AppGroup"/> with <see cref="AppGroup.ID"/> of <paramref name="groupID"/></li>
        ///         <li><see cref="AppPermission"/> with <see cref="AppPermission.ID"/> of <paramref name="permission"/></li>
        ///     </ul>
        /// </response>
        /// <exception cref="SystemException"></exception>
        [HttpPost("{groupID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        [Authorize]
        public async Task<ApiResponse<ulong>> AddPermission(ulong groupID, [FromQuery] string permission) {
            AppGroup? group = await _GroupDb.GetByID(groupID);
            if (group == null) {
                return ApiNotFound<ulong>($"{nameof(AppGroup)} {groupID}");
            }

            AppPermission? p = AppPermission.All.FirstOrDefault(iter => iter.ID.ToLower() == permission.ToLower());
            if (p == null) {
                return ApiNotFound<ulong>($"{nameof(AppPermission)} {permission}");
            }

            List<AppGroupPermission> perms = await _PermissionRepository.GetByGroupID(groupID);
            if (perms.FirstOrDefault(iter => iter.Permission.ToLower() == permission.ToLower()) != null) {
                return ApiBadRequest<ulong>($"{nameof(AppGroup)} {groupID} already has permssion {permission}");
            }

            AppAccount currentUser = await _CurrentAccount.Get() ?? throw new SystemException("no current user");

            AppGroupPermission perm = new();
            perm.GroupID = groupID;
            perm.Permission = permission;
            perm.GrantedByID = currentUser.ID;

            ulong ID = await _PermissionRepository.Insert(perm);

            return ApiOk(ID);
        }

    }
}
