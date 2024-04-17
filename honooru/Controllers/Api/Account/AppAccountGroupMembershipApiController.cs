using honooru.Code;
using honooru.Models;
using honooru.Models.Internal;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Controllers.Api.Account {

    [Route("/api/group-membership")]
    [ApiController]
    public class AppAccountGroupMembershipApiController : ApiControllerBase {

        private readonly ILogger<AppAccountGroupMembershipApiController> _Logger;
        private readonly AppCurrentAccount _CurrentUser;
        private readonly AppAccountGroupMembershipRepository _MembershipRepository;
        private readonly AppAccountDbStore _AccountDb;
        private readonly AppGroupRepository _GroupRepository;

        public AppAccountGroupMembershipApiController(ILogger<AppAccountGroupMembershipApiController> logger,
            AppAccountGroupMembershipRepository membershipRepository, AppAccountDbStore accountDb,
            AppGroupRepository groupRepository, AppCurrentAccount currentUser) {

            _Logger = logger;

            _MembershipRepository = membershipRepository;
            _AccountDb = accountDb;
            _GroupRepository = groupRepository;
            _CurrentUser = currentUser;
        }

        [HttpGet("account/{accountID}")]
        public async Task<ApiResponse<List<AppAccountGroupMembership>>> GetByAccountID(ulong accountID) {
            List<AppAccountGroupMembership> membership = await _MembershipRepository.GetByAccountID(accountID);

            return ApiOk(membership);
        }

        [HttpGet("group/{groupID}")]
        public async Task<ApiResponse<List<AppAccountGroupMembership>>> GetByGroupID(ulong groupID) {
            List<AppAccountGroupMembership> membership = await _MembershipRepository.GetByGroupID(groupID);

            return ApiOk(membership);
        }

        [HttpPost("{groupID}/{accountID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> AddUser(ulong groupID, ulong accountID) {
            AppAccount? currentUser = await _CurrentUser.Get();
            if (currentUser == null) {
                return ApiAuthorize();
            }

            _Logger.LogInformation($"user is being added to a group [groupID={groupID}] [accountID={accountID}] [currentUser={currentUser.ID}/{currentUser.Name}]");

            AppGroup? group = await _GroupRepository.GetByID(groupID);
            if (group == null) {
                return ApiNotFound($"{nameof(AppGroup)} {groupID}");
            }

            AppAccount? account = await _AccountDb.GetByID(accountID, CancellationToken.None);
            if (account == null) {
                return ApiNotFound($"{nameof(AppAccount)} {accountID}");
            }

            AppAccountGroupMembership membership = new();
            membership.AccountID = accountID;
            membership.GroupID = groupID;
            membership.GrantedByAccountID = currentUser.ID;
            membership.Timestamp = DateTime.UtcNow;

            membership.ID = await _MembershipRepository.Insert(membership);

            return ApiOk();
        }

        [HttpDelete("{groupID}/{accountID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> RemoveUser(ulong groupID, ulong accountID) {
            AppAccount? currentUser = await _CurrentUser.Get();
            if (currentUser == null) {
                return ApiAuthorize();
            }

            _Logger.LogInformation($"user is being removed from group [groupID={groupID}] [accountID={accountID}] [currentUser={currentUser.ID}/{currentUser.Name}]");

            List<AppAccountGroupMembership> memberships = await _MembershipRepository.GetByAccountID(accountID);
            AppAccountGroupMembership? membership = memberships.FirstOrDefault(iter => iter.GroupID == groupID);

            if (membership == null) {
                return ApiBadRequest($"account {accountID} is not a member of group {groupID}");
            }

            await _MembershipRepository.Delete(membership);

            return ApiOk();
        }

    }
}
