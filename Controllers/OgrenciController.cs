using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Data;
using StajEvrakDogrulama.Models;
using StajEvrakDogrulama.Services;

namespace StajEvrakDogrulama.Controllers
{
    [Authorize(Roles = "ROLE_STUDENT")]
    public class OgrenciController : Controller
    {
        private readonly VeritabaniBaglami _db;
        private readonly Ek16KontrolService _kontrolService;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] IzinliUzantilar =
        {
            ".pdf",
            ".doc",
            ".docx",
            ".jpg",
            ".jpeg",
            ".png"
        };

        public OgrenciController(
            VeritabaniBaglami db,
            Ek16KontrolService kontrolService,
            IWebHostEnvironment environment)
        {
            _db = db;
            _kontrolService = kontrolService;
            _environment = environment;
        }

        // --------------------------------------------------
        // ÖĞRENCİ ID BİLGİSİNİ ALMA
        // --------------------------------------------------

        private bool OgrenciIdAl(out int userId)
        {
            var idText =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(idText, out userId);
        }

        // --------------------------------------------------
        // ÖĞRENCİ ANA SAYFASI
        // --------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!OgrenciIdAl(out var userId))
                return Forbid();

            var student = await _db.Ogrenciler
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // --------------------------------------------------
        // ÖĞRENCİNİN SON BELGESİNİ GETİRME
        // --------------------------------------------------

        private async Task<Ek16Belge?> SonBelgeyiGetirAsync(int userId)
        {
            return await _db.Ek16Belgeler
                .AsNoTracking()
                .Where(x => x.OgrenciId == userId)
                .OrderByDescending(x => x.YuklenmeTarihi)
                .FirstOrDefaultAsync();
        }

        // --------------------------------------------------
        // EK-16 YÜKLEME SAYFASINI AÇMA
        // --------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Ek16Yukle()
        {
            if (!OgrenciIdAl(out var userId))
                return Forbid();

            var sonBelge =
                await SonBelgeyiGetirAsync(userId);

            return View(sonBelge);
        }

        // --------------------------------------------------
        // EK-16 DOSYASI YÜKLEME
        // --------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ek16Yukle(IFormFile dosya)
        {
            if (!OgrenciIdAl(out var userId))
                return Forbid();

            // Dosya seçilip seçilmediğini kontrol et
            if (dosya == null || dosya.Length == 0)
            {
                ViewBag.Hata =
                    "Lütfen yüklemek istediğiniz Ek-16 dosyasını seçin.";

                return View(
                    await SonBelgeyiGetirAsync(userId));
            }

            // Dosya uzantısını al
            var uzanti = Path.GetExtension(dosya.FileName)
                .ToLowerInvariant();

            // İzin verilen formatları kontrol et
            if (!IzinliUzantilar.Contains(
                    uzanti,
                    StringComparer.OrdinalIgnoreCase))
            {
                ViewBag.Hata =
                    "Geçersiz dosya formatı. " +
                    "PDF, DOC, DOCX, JPG, JPEG veya PNG dosyası yükleyebilirsiniz.";

                return View(
                    await SonBelgeyiGetirAsync(userId));
            }

            // wwwroot/uploads/ek16 klasörünü oluştur
            var webRootPath = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            var uploadsFolder = Path.Combine(
                webRootPath,
                "uploads",
                "ek16");

            Directory.CreateDirectory(uploadsFolder);

            // Dosyaya benzersiz bir isim ver
            var yeniDosyaAdi =
                $"{Guid.NewGuid():N}{uzanti}";

            var fizikselDosyaYolu = Path.Combine(
                uploadsFolder,
                yeniDosyaAdi);

            // Dosyayı sunucuya kaydet
            try
            {
                await using var stream = new FileStream(
                    fizikselDosyaYolu,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

                await dosya.CopyToAsync(stream);
            }
            catch (IOException)
            {
                ViewBag.Hata =
                    "Dosya sunucuya kaydedilemedi. Lütfen tekrar deneyin.";

                return View(
                    await SonBelgeyiGetirAsync(userId));
            }
            catch (UnauthorizedAccessException)
            {
                ViewBag.Hata =
                    "Dosya kayıt klasörüne erişilemiyor.";

                return View(
                    await SonBelgeyiGetirAsync(userId));
            }

            // Veritabanına kaydedilecek belge nesnesi
            var belge = new Ek16Belge
            {
                OgrenciId = userId,

                DosyaAdi = Path.GetFileName(dosya.FileName),

                DosyaYolu = $"/uploads/ek16/{yeniDosyaAdi}",

                DosyaTuru = uzanti,

                YuklenmeTarihi = DateTime.Now,

                Durum = Ek16Belge.DurumKontrolBekliyor,

                KontrolRaporu = string.Empty
            };

            // Belgeyi veritabanına ekle
            _db.Ek16Belgeler.Add(belge);

            await _db.SaveChangesAsync();

            // Kaydedilen belgeyi kontrol servisine gönder
            await _kontrolService.KontrolEt(belge.Id);

            TempData["Mesaj"] =
                "Ek-16 dosyanız başarıyla yüklendi ve kontrol işlemine gönderildi.";

            return RedirectToAction(nameof(Ek16Yukle));
        }

        // --------------------------------------------------
        // PDF HAM METİN TESTİ
        // --------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> PdfMetinTest()
        {
            if (!OgrenciIdAl(out var userId))
                return Forbid();

            var belge = await _db.Ek16Belgeler
                .Where(x => x.OgrenciId == userId)
                .OrderByDescending(x => x.YuklenmeTarihi)
                .FirstOrDefaultAsync();

            if (belge == null)
                return Content("Ek-16 belgesi bulunamadı.");

            var webRootPath = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            var fizikselDosyaYolu = Path.Combine(
                webRootPath,
                belge.DosyaYolu
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(fizikselDosyaYolu))
            {
                return Content(
                    $"PDF bulunamadı:\n{fizikselDosyaYolu}");
            }

            if (!belge.DosyaTuru.Equals(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Content(
                    "Ham metin testi yalnızca PDF dosyaları için yapılabilir.");
            }

            using var document =
                UglyToad.PdfPig.PdfDocument.Open(
                    fizikselDosyaYolu);

            var sb =
                new System.Text.StringBuilder();

            foreach (var sayfa in document.GetPages())
            {
                sb.AppendLine("========== SAYFA ==========");
                sb.AppendLine(sayfa.Text);
                sb.AppendLine();
            }

            return Content(sb.ToString());
        }
    }
}