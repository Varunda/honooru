using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using honooru.Code;
using honooru.Models.Internal;
using honooru.Services;
using System.Threading.Tasks;
using honooru.Models;

namespace honooru.Controllers {

    [PermissionNeeded(AppPermission.APP_VIEW)]
    public class HomeController : Controller {

        private readonly ILogger<HomeController> _Logger;

        private readonly IHttpContextAccessor _HttpContextAccessor;
        private readonly HttpUtilService _HttpUtil;
        private readonly AppCurrentAccount _CurrentUser;

        public HomeController(ILogger<HomeController> logger,
            IHttpContextAccessor httpContextAccessor, HttpUtilService httpUtil,
            AppCurrentAccount currentUser) {

            _HttpContextAccessor = httpContextAccessor;
            _HttpUtil = httpUtil;
            _Logger = logger;
            _CurrentUser = currentUser;
        }

        public IActionResult Index() {
            return View();
        }

        public IActionResult Settings() {
            return View();
        }

        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public IActionResult AccountManagement() {
            return View();
        }

        public IActionResult Health() {
            return View();
        }

        public IActionResult Posts() {
            return View();
        }

        public IActionResult Upload() {
            return View();
        }

        public IActionResult Post() {
            return View();
        }

        public IActionResult Tag() {
            return View();
        }

        public IActionResult TagType() {
            return View();
        }

        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public IActionResult Admin() {
            return View();
        }

    }
}
