using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Data;
using StajEvrakDogrulama.Models;
using StajEvrakDogrulama.Services;

namespace StajEvrakDogrulama.Controllers
{
    [Authorize(Roles = "ROLE_ADMIN")]
    public class YoneticiController : Controller
    {
        private readonly VeritabaniBaglami _db;
        private readonly StudentImportService _studentImportService;

        public YoneticiController(
            VeritabaniBaglami db,
            StudentImportService studentImportService)
        {
            _db = db;
            _studentImportService = studentImportService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await DashboardModeliniGetirAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (file is null)
            {
                ViewBag.UploadError = "Lütfen bir CSV veya XLSX dosyası seçin.";
                return View(nameof(Index), await DashboardModeliniGetirAsync(cancellationToken));
            }

            ViewBag.ImportResult = await _studentImportService.ImportAsync(
                file,
                cancellationToken);

            var model = await DashboardModeliniGetirAsync(cancellationToken);
            return View(nameof(Index), model);
        }

        [HttpGet]
        public IActionResult CsvTemplate()
        {
            const string csv =
                "AdSoyad,KullaniciAdi,Eposta,TcKimlikNo,OgrenciNo,Bolum,TelefonNo,IkametgahAdresi,DogumTarihi,TeslimYili,OncekiStajGunSayisi,Uyruk,SaglikYardimiDurumu\r\n";

            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(csv))
                .ToArray();

            return File(bytes, "text/csv; charset=utf-8", "ogrenci-sablonu.csv");
        }

        private async Task<AdminDashboardViewModel> DashboardModeliniGetirAsync(
            CancellationToken cancellationToken = default)
        {
            var students = await _db.Ogrenciler
                .AsNoTracking()
                .OrderBy(student => student.AdSoyad)
                .ToListAsync(cancellationToken);

            return new AdminDashboardViewModel
            {
                Students = students
            };
        }
    }
}