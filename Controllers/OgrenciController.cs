using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StajEvrakDogrulama.Controllers
{
    [Authorize(Roles = "ROLE_STUDENT")]
    public class OgrenciController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}