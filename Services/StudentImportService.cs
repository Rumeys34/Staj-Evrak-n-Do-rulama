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

        // ---------------------------------------------------------
        // 1. DOSYA KONTROLÜ
        // ---------------------------------------------------------

        if (file.Length == 0 || file.Length > MaxFileSize)
        {
            result.AddError(
                "Dosya boş olamaz ve 5 MB'dan büyük olamaz.");

            return result;
        }

        var uzanti =
            Path.GetExtension(file.FileName);

        var parser =
            _parsers.FirstOrDefault(
                item => item.CanParse(uzanti));

        if (parser is null)
        {
            result.AddError(
                "Yalnızca .csv veya .xlsx dosyaları desteklenir.");

            return result;
        }

        // ---------------------------------------------------------
        // 2. DOSYAYI OKU
        // ---------------------------------------------------------

        IReadOnlyList<StudentImportRow> rows;

        try
        {
            await using var stream =
                file.OpenReadStream();

            rows =
                parser.Parse(stream);
        }
        catch (Exception ex) when (
            ex is InvalidDataException
            or IOException
            or FormatException
            or ArgumentException)
        {
            result.AddError(
                $"Dosya okunamadı: {ex.Message}");

            return result;
        }

        result.SetTotalRows(rows.Count);

        if (rows.Count == 0)
        {
            result.AddError(
                "Dosyada aktarılacak öğrenci satırı bulunamadı.");

            return result;
        }

        // ---------------------------------------------------------
        // 3. DOSYA İÇİNDE TEKRAR KONTROLÜ
        // ---------------------------------------------------------

        var usernames =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var emails =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var studentNumbers =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var identityNumbers =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var rowErrors =
                new List<string>();

            // Ad Soyad
            if (string.IsNullOrWhiteSpace(
                    row.AdSoyad))
            {
                rowErrors.Add(
                    "AdSoyad zorunludur.");
            }

            // Kullanıcı adı
            if (string.IsNullOrWhiteSpace(
                    row.KullaniciAdi))
            {
                rowErrors.Add(
                    "KullaniciAdi zorunludur.");
            }
            else if (!usernames.Add(
                         row.KullaniciAdi.Trim()))
            {
                rowErrors.Add(
                    "KullaniciAdi dosyada tekrarlanıyor.");
            }

            // E-posta
            if (string.IsNullOrWhiteSpace(
                    row.Eposta) ||
                !new EmailAddressAttribute()
                    .IsValid(row.Eposta.Trim()))
            {
                rowErrors.Add(
                    "Eposta eksik veya geçersiz.");
            }
            else if (!emails.Add(
                         row.Eposta.Trim()))
            {
                rowErrors.Add(
                    "Eposta dosyada tekrarlanıyor.");
            }

            // TC Kimlik
            var tc =
                row.TcKimlikNo?.Trim() ?? "";

            if (tc.Length != 11 ||
                !tc.All(char.IsDigit))
            {
                rowErrors.Add(
                    "TcKimlikNo 11 rakam olmalıdır.");
            }
            else
            {
                if (!identityNumbers.Add(tc))
                {
                    rowErrors.Add(
                        "TcKimlikNo dosyada tekrarlanıyor.");
                }
            }

            // Öğrenci numarası
            var studentNumber =
                row.OgrenciNo?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(
                    studentNumber))
            {
                rowErrors.Add(
                    "OgrenciNo zorunludur.");
            }
            else if (!studentNumbers.Add(
                         studentNumber))
            {
                rowErrors.Add(
                    "OgrenciNo dosyada tekrarlanıyor.");
            }

            // Doğum tarihi
            if (row.DogumTarihi is null)
            {
                rowErrors.Add(
                    "DogumTarihi eksik veya geçersiz.");
            }

            // Önceki staj gün sayısı
            if (row.OncekiStajGunSayisi < 0)
            {
                rowErrors.Add(
                    "OncekiStajGunSayisi negatif olamaz.");
            }

            // Satır sonucu
            if (rowErrors.Count == 0)
            {
                result.AddValidRow();
            }
            else
            {
                result.AddError(
                    $"Satır {row.RowNumber}: " +
                    $"{string.Join(" ", rowErrors)}");
            }
        }

        // ---------------------------------------------------------
        // 4. DOSYADA HATA VARSA HİÇBİR ŞEY KAYDETME
        // ---------------------------------------------------------

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // ---------------------------------------------------------
        // 5. VERİTABANINDA KONTROL
        // ---------------------------------------------------------
        //
        // Buradaki önemli değişiklik:
        //
        // Daha önce mevcut öğrenci varsa hata veriyorduk.
        //
        // Artık:
        //
        // - Öğrenci varsa -> GÜNCELLE
        // - Öğrenci yoksa  -> YENİ EKLE
        //
        // Eşleşme önceliği:
        // 1. Öğrenci numarası
        // 2. TC Kimlik numarası
        // 3. Kullanıcı adı
        // 4. E-posta
        //
        // ---------------------------------------------------------

        foreach (var row in rows)
        {
            var username =
                row.KullaniciAdi.Trim();

            var email =
                row.Eposta.Trim();

            var studentNumber =
                row.OgrenciNo.Trim();

            var tc =
                row.TcKimlikNo.Trim();

            var mevcutOgrenci =
                await _db.Ogrenciler
                    .FirstOrDefaultAsync(
                        student =>
                            student.OgrenciNo == studentNumber ||
                            student.TcKimlikNo == tc ||
                            student.KullaniciAdi == username ||
                            student.Eposta == email,
                        cancellationToken);

            // -----------------------------------------------------
            // AYNI KAYIT İÇİNDE ÇAKIŞMA VAR MI?
            // -----------------------------------------------------

            if (mevcutOgrenci != null)
            {
                var baskaOgrenci =
                    await _db.Ogrenciler
                        .AnyAsync(
                            student =>
                                student.Id != mevcutOgrenci.Id &&
                                (
                                    student.OgrenciNo == studentNumber ||
                                    student.TcKimlikNo == tc ||
                                    student.KullaniciAdi == username ||
                                    student.Eposta == email
                                ),
                            cancellationToken);

                if (baskaOgrenci)
                {
                    result.AddError(
                        $"Satır {row.RowNumber}: " +
                        "öğrenci numarası, T.C. kimlik numarası, " +
                        "kullanıcı adı veya e-posta başka bir öğrenciye ait.");

                    continue;
                }

                continue;
            }

            // -----------------------------------------------------
            // YENİ ÖĞRENCİ İÇİN KULLANICI ÇAKIŞMASI
            // -----------------------------------------------------

            var accountExists =
                await _db.Kullanicilar
                    .AnyAsync(
                        user =>
                            user.KullaniciAdi == username ||
                            user.Eposta == email,
                        cancellationToken);

            if (accountExists)
            {
                result.AddError(
                    $"Satır {row.RowNumber}: " +
                    "kullanıcı adı veya e-posta zaten kayıtlı.");
            }
        }

        // ---------------------------------------------------------
        // 6. ÇAKIŞMA HATASI VARSA KAYDETME
        // ---------------------------------------------------------

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // ---------------------------------------------------------
        // 7. EKLE / GÜNCELLE
        // ---------------------------------------------------------

        foreach (var row in rows)
        {
            var username =
                row.KullaniciAdi.Trim();

            var email =
                row.Eposta.Trim();

            var studentNumber =
                row.OgrenciNo.Trim();

            var tc =
                row.TcKimlikNo.Trim();

            var mevcutOgrenci =
                await _db.Ogrenciler
                    .FirstOrDefaultAsync(
                        student =>
                            student.OgrenciNo == studentNumber ||
                            student.TcKimlikNo == tc ||
                            student.KullaniciAdi == username ||
                            student.Eposta == email,
                        cancellationToken);

            // -----------------------------------------------------
            // MEVCUT ÖĞRENCİ VARSA GÜNCELLE
            // -----------------------------------------------------

            if (mevcutOgrenci != null)
            {
                mevcutOgrenci.AdSoyad =
                    row.AdSoyad.Trim();

                mevcutOgrenci.KullaniciAdi =
                    row.KullaniciAdi.Trim();

                mevcutOgrenci.Eposta =
                    row.Eposta.Trim();

                mevcutOgrenci.TcKimlikNo =
                    row.TcKimlikNo.Trim();

                mevcutOgrenci.OgrenciNo =
                    row.OgrenciNo.Trim();

                mevcutOgrenci.Bolum =
                    row.Bolum?.Trim() ?? "";

                mevcutOgrenci.TelefonNo =
                    row.TelefonNo?.Trim() ?? "";

                mevcutOgrenci.IkametgahAdresi =
                    row.IkametgahAdresi?.Trim() ?? "";

                mevcutOgrenci.DogumTarihi =
                    row.DogumTarihi!.Value;

                mevcutOgrenci.TeslimYili =
                    row.TeslimYili;

                mevcutOgrenci.OncekiStajGunSayisi =
                    row.OncekiStajGunSayisi;

                mevcutOgrenci.Uyruk =
                    row.Uyruk?.Trim() ?? "";

                mevcutOgrenci.SaglikYardimiDurumu =
                    row.SaglikYardimiDurumu?.Trim() ?? "";

                // Mevcut öğrencinin şifresini değiştirmiyoruz.
                // Böylece öğrenci tekrar içe aktarıldığında
                // mevcut giriş şifresi bozulmaz.

                continue;
            }

            // -----------------------------------------------------
            // YENİ ÖĞRENCİ
            // -----------------------------------------------------

            var temporaryPassword =
                Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(12))
                .Replace("+", "A")
                .Replace("/", "B")
                .TrimEnd('=');

            var student =
                new Ogrenci
                {
                    AdSoyad =
                        row.AdSoyad.Trim(),

                    KullaniciAdi =
                        row.KullaniciAdi.Trim(),

                    Eposta =
                        row.Eposta.Trim(),

                    SifreHash =
                        SifreYardimcisi.SifreyiHashle(
                            temporaryPassword),

                    Rol =
                        "ROLE_STUDENT",

                    TcKimlikNo =
                        row.TcKimlikNo.Trim(),

                    OgrenciNo =
                        row.OgrenciNo.Trim(),

                    Bolum =
                        row.Bolum?.Trim() ?? "",

                    TelefonNo =
                        row.TelefonNo?.Trim() ?? "",

                    IkametgahAdresi =
                        row.IkametgahAdresi?.Trim() ?? "",

                    DogumTarihi =
                        row.DogumTarihi!.Value,

                    TeslimYili =
                        row.TeslimYili,

                    OncekiStajGunSayisi =
                        row.OncekiStajGunSayisi,

                    Uyruk =
                        row.Uyruk?.Trim() ?? "",

                    SaglikYardimiDurumu =
                        row.SaglikYardimiDurumu?.Trim() ?? ""
                };

            _db.Ogrenciler.Add(student);

            // Sadece yeni oluşturulan hesap için
            // geçici şifreyi sonuç nesnesine ekle.
            result.AddAccount(
                new ImportedStudentAccount(
                    row.OgrenciNo,
                    row.KullaniciAdi,
                    temporaryPassword));
        }

        // ---------------------------------------------------------
        // 8. VERİTABANINA KAYDET
        // ---------------------------------------------------------

        await _db.SaveChangesAsync(
            cancellationToken);

        return result;
    }
}