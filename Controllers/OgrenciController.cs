using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Data;

namespace StajEvrakDogrulama.Controllers
{
    [Authorize(Roles = "ROLE_STUDENT")]
    public class OgrenciController : Controller
    {
        private readonly VeritabaniBaglami _db;

        public OgrenciController(VeritabaniBaglami db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var idText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idText, out var userId))
                return Forbid();

            var student = await _db.Ogrenciler
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == userId);

            if (student is null)
                return NotFound();

            return View(student);
        }
    }
}