using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace honooru.Controllers {

    [AllowAnonymous]
    public class UnauthorizedController : Controller {

        public UnauthorizedController() {

        }

        public IActionResult Index() {
            return View();
        }

    }
}
