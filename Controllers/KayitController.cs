
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Data;
using StajEvrakDogrulama.Models;
using StajEvrakDogrulama.Yardimcilar;

namespace StajEvrakDogrulama.Controllers
{
    public class KayitController : Controller
    {
        private readonly VeritabaniBaglami _veritabani;

        public KayitController(VeritabaniBaglami veritabani)
        {
            _veritabani = veritabani;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new Kayit());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Kayit model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool kullaniciVarMi = await _veritabani.Kullanicilar
                .AnyAsync(k =>
                    k.KullaniciAdi == model.KullaniciAdi ||
                    k.Eposta == model.Eposta);

            if (kullaniciVarMi)
            {
                ModelState.AddModelError(
                    "",
                    "Bu kullanıcı adı veya e-posta zaten kullanılıyor.");

                return View(model);
            }

            var ogrenci = new Ogrenci
            {
                AdSoyad = model.AdSoyad,
                KullaniciAdi = model.KullaniciAdi,
                Eposta = model.Eposta,

                SifreHash = SifreYardimcisi.SifreyiHashle(
                    model.Sifre),

                Rol = "ROLE_STUDENT"
            };

            _veritabani.Kullanicilar.Add(ogrenci);

            await _veritabani.SaveChangesAsync();

            TempData["Mesaj"] = "Kaydınız başarıyla oluşturuldu.";

            return RedirectToAction("Index", "Giris");
        }
    }
}
