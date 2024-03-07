using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using honooru.Code;
using honooru.Models.Internal;
using honooru.Services;

namespace honooru.Controllers {

    public class HomeController : Controller {

        private readonly ILogger<HomeController> _Logger;

        private readonly IHttpContextAccessor _HttpContextAccessor;
        private readonly HttpUtilService _HttpUtil;

        public HomeController(ILogger<HomeController> logger,
            IHttpContextAccessor httpContextAccessor, HttpUtilService httpUtil) {

            _HttpContextAccessor = httpContextAccessor;
            _HttpUtil = httpUtil;
            _Logger = logger;
        }

        public IActionResult Index() {
            return View();
        }

        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        [Authorize]
        public IActionResult AccountManagement() {
            return View();
        }

        public IActionResult Health() {
            return View();
        }

    }
}
