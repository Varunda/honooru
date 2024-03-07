using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Models.Internal;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/permission")]
    public class AppPermissionApiController : ApiControllerBase {

        private readonly ILogger<AppPermissionApiController> _Logger;

        public AppPermissionApiController(ILogger<AppPermissionApiController> logger) {
            _Logger = logger;
        }

        /// <summary>
        ///     Get all permissions available to users
        /// </summary>
        /// <response code="200">
        ///     A list of <see cref="AppPermission"/>s
        /// </response>
        [HttpGet]
        public ApiResponse<List<AppPermission>> GetAll() {
            return ApiOk(AppPermission.All);
        }

    }
}
