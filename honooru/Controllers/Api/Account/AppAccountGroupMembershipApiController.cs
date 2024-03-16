using honooru.Models;
using honooru.Models.Internal;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Controllers.Api.Account {

    [Route("/api/group-membership")]
    [ApiController]
    public class AppAccountGroupMembershipApiController : ApiControllerBase {

        private readonly ILogger<AppAccountGroupMembershipApiController> _Logger;
        private readonly AppAccountGroupMembershipRepository _MembershipRepository;

        public AppAccountGroupMembershipApiController(ILogger<AppAccountGroupMembershipApiController> logger,
            AppAccountGroupMembershipRepository membershipRepository) {

            _Logger = logger;

            _MembershipRepository = membershipRepository;
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

    }
}
