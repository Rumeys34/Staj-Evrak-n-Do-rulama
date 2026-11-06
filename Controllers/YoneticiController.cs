using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StajEvrakDogrulama.Controllers
{
    [Authorize(Roles = "ROLE_ADMIN")]
    public class YoneticiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}