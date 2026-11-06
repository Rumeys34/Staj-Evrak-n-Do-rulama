using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Data;
using StajEvrakDogrulama.Models;
using StajEvrakDogrulama.Yardimcilar;

namespace StajEvrakDogrulama.Controllers
{
    public class GirisController : Controller
    {
        private readonly VeritabaniBaglami _veritabani;

        public GirisController(VeritabaniBaglami veritabani)
        {
            _veritabani = veritabani;
        }

        // Giriş sayfasını açar
        [HttpGet]
        public IActionResult Index()
        {
            return View(new Login());
        }

        // Kullanıcı giriş işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Login model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var kullanici = await _veritabani.Kullanicilar
                .FirstOrDefaultAsync(k =>
                    k.KullaniciAdi == model.KullaniciAdi ||
                    k.Eposta == model.KullaniciAdi);

            if (kullanici == null ||
                !SifreYardimcisi.SifreDogruMu(
                    model.Sifre,
                    kullanici.SifreHash))
            {
                ModelState.AddModelError(
                    "",
                    "Kullanıcı adı veya şifre hatalı.");

                return View(model);
            }

            if (kullanici.Rol != "ROLE_STUDENT" &&
                kullanici.Rol != "ROLE_ADMIN")
            {
                ModelState.AddModelError(
                    "",
                    "Kullanıcı rolü tanınmıyor.");

                return View(model);
            }

            var bilgiler = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    kullanici.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    kullanici.AdSoyad),

                new Claim(
                    ClaimTypes.Role,
                    kullanici.Rol)
            };

            var kimlik = new ClaimsIdentity(
                bilgiler,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var kullaniciBilgisi = new ClaimsPrincipal(kimlik);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                kullaniciBilgisi);

            // Öğrenci ise öğrenci paneline gönder
            if (kullanici.Rol == "ROLE_STUDENT")
            {
                return RedirectToAction("Index", "Ogrenci");
            }

            // Admin ise yönetici paneline gönder
            return RedirectToAction("Index", "Yonetici");
        }

        // Çıkış işlemi
        [HttpGet]
        public async Task<IActionResult> Cikis()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Giris");
        }
    }
}