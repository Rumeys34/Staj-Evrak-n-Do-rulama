using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Data;
using StajEvrakDogrulama.Models;
using StajEvrakDogrulama.Yardimcilar;

namespace StajEvrakDogrulama.Services;

public sealed class StudentImportService
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly VeritabaniBaglami _db;
    private readonly IEnumerable<IStudentImportParser> _parsers;

    public StudentImportService(
        VeritabaniBaglami db,
        IEnumerable<IStudentImportParser> parsers)
    {
        _db = db;
        _parsers = parsers;
    }

    public async Task<StudentImportResult> ImportAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var result = new StudentImportResult();

        if (file.Length == 0 || file.Length > MaxFileSize)
        {
            result.AddError("Dosya boş olamaz ve 5 MB'dan büyük olamaz.");
            return result;
        }

        var parser = _parsers.FirstOrDefault(
            item => item.CanParse(Path.GetExtension(file.FileName)));

        if (parser is null)
        {
            result.AddError("Yalnızca .csv veya .xlsx dosyaları desteklenir.");
            return result;
        }

        IReadOnlyList<StudentImportRow> rows;

        try
        {
            await using var stream = file.OpenReadStream();
            rows = parser.Parse(stream);
        }
        catch (Exception ex) when (
            ex is InvalidDataException or IOException or FormatException or ArgumentException)
        {
            result.AddError($"Dosya okunamadı: {ex.Message}");
            return result;
        }

        result.SetTotalRows(rows.Count);

        if (rows.Count == 0)
        {
            result.AddError("Dosyada aktarılacak öğrenci satırı bulunamadı.");
            return result;
        }

        var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var studentNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var identityNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var rowErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(row.AdSoyad))
                rowErrors.Add("AdSoyad zorunludur.");

            if (string.IsNullOrWhiteSpace(row.KullaniciAdi))
                rowErrors.Add("KullaniciAdi zorunludur.");
            else if (!usernames.Add(row.KullaniciAdi.Trim()))
                rowErrors.Add("KullaniciAdi dosyada tekrarlanıyor.");

            if (string.IsNullOrWhiteSpace(row.Eposta) ||
                !new EmailAddressAttribute().IsValid(row.Eposta))
                rowErrors.Add("Eposta eksik veya geçersiz.");
            else if (!emails.Add(row.Eposta.Trim()))
                rowErrors.Add("Eposta dosyada tekrarlanıyor.");

            if (row.TcKimlikNo.Length != 11 ||
                !row.TcKimlikNo.All(char.IsDigit))
                rowErrors.Add("TcKimlikNo 11 rakam olmalıdır.");
            else if (!identityNumbers.Add(row.TcKimlikNo))
                rowErrors.Add("TcKimlikNo dosyada tekrarlanıyor.");

            if (string.IsNullOrWhiteSpace(row.OgrenciNo))
                rowErrors.Add("OgrenciNo zorunludur.");
            else if (!studentNumbers.Add(row.OgrenciNo.Trim()))
                rowErrors.Add("OgrenciNo dosyada tekrarlanıyor.");

            if (row.DogumTarihi is null)
                rowErrors.Add("DogumTarihi eksik veya geçersiz.");

            if (row.OncekiStajGunSayisi < 0)
                rowErrors.Add("OncekiStajGunSayisi negatif olamaz.");

            if (rowErrors.Count == 0)
                result.AddValidRow();
            else
                result.AddError(
                    $"Satır {row.RowNumber}: {string.Join(" ", rowErrors)}");
        }

        // Dosyada hata varsa hiçbir öğrenci kaydedilmez.
        if (result.Errors.Count > 0)
            return result;

        // Veritabanında mevcut hesap veya öğrenci bilgisi var mı kontrol et.
        foreach (var row in rows)
        {
            var username = row.KullaniciAdi.Trim();
            var email = row.Eposta.Trim();
            var studentNumber = row.OgrenciNo.Trim();

            var accountExists = await _db.Kullanicilar.AnyAsync(
                user => user.KullaniciAdi == username ||
                        user.Eposta == email,
                cancellationToken);

            var studentExists = await _db.Ogrenciler.AnyAsync(
                student => student.OgrenciNo == studentNumber ||
                           student.TcKimlikNo == row.TcKimlikNo,
                cancellationToken);

            if (accountExists || studentExists)
            {
                result.AddError(
                    $"Satır {row.RowNumber}: kullanıcı adı, e-posta, öğrenci numarası veya T.C. kimlik numarası zaten kayıtlı.");
            }
        }

        // Çakışma varsa dosyanın hiçbir satırı kaydedilmez.
        if (result.Errors.Count > 0)
            return result;

        foreach (var row in rows)
        {
            var temporaryPassword = Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(12))
                .Replace("+", "A")
                .Replace("/", "B")
                .TrimEnd('=');

            var student = new Ogrenci
            {
                AdSoyad = row.AdSoyad.Trim(),
                KullaniciAdi = row.KullaniciAdi.Trim(),
                Eposta = row.Eposta.Trim(),
                SifreHash = SifreYardimcisi.SifreyiHashle(temporaryPassword),
                Rol = "ROLE_STUDENT",
                TcKimlikNo = row.TcKimlikNo,
                OgrenciNo = row.OgrenciNo.Trim(),
                Bolum = row.Bolum,
                TelefonNo = row.TelefonNo,
                IkametgahAdresi = row.IkametgahAdresi,
                DogumTarihi = row.DogumTarihi!.Value,
                TeslimYili = row.TeslimYili,
                OncekiStajGunSayisi = row.OncekiStajGunSayisi,
                Uyruk = row.Uyruk,
                SaglikYardimiDurumu = row.SaglikYardimiDurumu
            };

            _db.Ogrenciler.Add(student);

            result.AddAccount(new ImportedStudentAccount(
                row.OgrenciNo,
                row.KullaniciAdi,
                temporaryPassword));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }
}