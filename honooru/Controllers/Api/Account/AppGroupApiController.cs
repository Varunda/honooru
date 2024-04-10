using honooru.Code;
using honooru.Models;
using honooru.Models.Internal;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Controllers.Api.Account {

    [Route("/api/group")]
    [ApiController]
    public class AppGroupApiController : ApiControllerBase {

        private readonly ILogger<AppGroupApiController> _Logger;
        private readonly AppGroupRepository _AppGroupRepository;

        public AppGroupApiController(ILogger<AppGroupApiController> logger, AppGroupRepository appGroupRepository) {
            _Logger = logger;
            _AppGroupRepository = appGroupRepository;
        }

        [HttpGet]
        public async Task<ApiResponse<List<AppGroup>>> GetAll() {
            List<AppGroup> groups = await _AppGroupRepository.GetAll();

            return ApiOk(groups);
        }

        [HttpPost]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> Create([FromQuery] string name, [FromQuery] string hex) {
            AppGroup group = new();
            group.Name = name;

            _Logger.LogInformation($"creating new {nameof(AppGroup)} [name={name}]");
            group.ID = await _AppGroupRepository.Insert(group);

            return ApiOk();
        }

    }
}
