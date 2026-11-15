using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Data;
using StajEvrakDogrulama.Models;
using UglyToad.PdfPig;

namespace StajEvrakDogrulama.Services
{
    public sealed class Ek16KontrolService
    {
        private readonly VeritabaniBaglami _db;

        private static readonly string[] IzinliUzantilar =
        {
            ".pdf",
            ".doc",
            ".docx",
            ".jpg",
            ".jpeg",
            ".png"
        };

        public Ek16KontrolService(VeritabaniBaglami db)
        {
            _db = db;
        }

        public async Task KontrolEt(int belgeId)
        {
            var belge = await _db.Ek16Belgeler
                .FirstOrDefaultAsync(x => x.Id == belgeId);

            if (belge == null)
                return;

            belge.Durum = "İnceleniyor";
            belge.KontrolRaporu = string.Empty;

            // Önceki kontrol sonuçlarını sıfırla.
            belge.FormatKontrolu = false;
            belge.OgrenciBilgileriKontrolu = false;
            belge.TcKimlikKontrolu = false;
            belge.IletisimBilgileriKontrolu = false;
            belge.TarihKontrolu = false;
            belge.StajGunKontrolu = false;
            belge.ImzaKontrolu = false;
            belge.KaseKontrolu = false;
            belge.BosAlanKontrolu = false;

            var rapor = new List<string>();

            // =====================================================
            // 1. BELGEYE AİT ÖĞRENCİYİ BUL
            // =====================================================

            var ogrenci = await _db.Ogrenciler
                .FirstOrDefaultAsync(x => x.Id == belge.OgrenciId);

            if (ogrenci == null)
            {
                belge.Durum = "Hatalı/Eksik";

                belge.KontrolRaporu =
                    "Öğrenci bilgileri veritabanında bulunamadı.";

                await _db.SaveChangesAsync();

                return;
            }

            // =====================================================
            // 2. FORMAT KONTROLÜ
            // =====================================================

            belge.FormatKontrolu =
                FormatKontrol(belge.DosyaTuru);

            rapor.Add(
                belge.FormatKontrolu
                    ? "Dosya formatı geçerli."
                    : "Dosya formatı geçersiz.");

            if (!belge.FormatKontrolu)
            {
                belge.Durum = "Hatalı/Eksik";

                belge.KontrolRaporu =
                    string.Join(
                        Environment.NewLine,
                        rapor);

                await _db.SaveChangesAsync();

                return;
            }

            // =====================================================
            // 3. PDF DIŞI DOSYALAR
            // =====================================================

            if (!belge.DosyaTuru.Equals(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                rapor.Add(
                    "Dosya formatı kabul edildi ancak Word ve resim " +
                    "dosyalarının içerik analizi henüz uygulanmadı.");

                belge.Durum = "İnceleniyor";

                belge.KontrolRaporu =
                    string.Join(
                        Environment.NewLine,
                        rapor);

                await _db.SaveChangesAsync();

                return;
            }

            // =====================================================
            // 4. PDF DOSYA YOLUNU BUL
            // =====================================================

            var webRoot =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot");

            var goreliYol =
                belge.DosyaYolu
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);

            var fizikselYol =
                Path.Combine(
                    webRoot,
                    goreliYol);

            if (!File.Exists(fizikselYol))
            {
                rapor.Add(
                    "PDF dosyası sunucuda bulunamadı.");

                belge.Durum = "Hatalı/Eksik";

                belge.KontrolRaporu =
                    string.Join(
                        Environment.NewLine,
                        rapor);

                await _db.SaveChangesAsync();

                return;
            }

            // =====================================================
            // 5. PDF METNİNİ OKU
            // =====================================================

            string hamMetin;

            try
            {
                hamMetin =
                    PdfMetniniOku(fizikselYol);
            }
            catch
            {
                rapor.Add(
                    "PDF dosyası okunamadı. Dosyanın bozuk veya " +
                    "şifreli olup olmadığını kontrol edin.");

                belge.Durum = "Hatalı/Eksik";

                belge.KontrolRaporu =
                    string.Join(
                        Environment.NewLine,
                        rapor);

                await _db.SaveChangesAsync();

                return;
            }

            if (string.IsNullOrWhiteSpace(hamMetin))
            {
                rapor.Add(
                    "PDF içerisinde okunabilir metin bulunamadı. " +
                    "Taranmış belge olabilir.");

                belge.Durum = "İnceleniyor";

                belge.KontrolRaporu =
                    string.Join(
                        Environment.NewLine,
                        rapor);

                await _db.SaveChangesAsync();

                return;
            }

            // PDF'deki boşluk, satır sonu ve özel karakterleri
            // kaldırılmış arama metni.
            var metin =
                AramaMetnineDonustur(hamMetin);

            rapor.Add(
                "PDF metni başarıyla okundu.");

            // =====================================================
            // 6. AD SOYAD
            // =====================================================

            var pdfAdSoyad =
                DegeriBul(
                    metin,
                    ogrenci.AdSoyad,
                    "ADISOYADI",
                    "ADSOYAD",
                    "ADI SOYADI");

            var adSoyadEslesiyor =
                EslesiyorMu(
                    pdfAdSoyad,
                    ogrenci.AdSoyad);

            rapor.Add(
                adSoyadEslesiyor
                    ? "Ad Soyad bilgisi bulundu ve eşleşiyor."
                    : $"Ad Soyad bulunamadı veya eşleşmiyor. " +
                      $"PDF: '{pdfAdSoyad}' | " +
                      $"Veritabanı: '{ogrenci.AdSoyad}'");

            // =====================================================
            // 7. ÖĞRENCİ NUMARASI
            // =====================================================

            var pdfOgrenciNo =
                DegeriBul(
                    metin,
                    ogrenci.OgrenciNo,
                    "OGRENCINUMARASI",
                    "OGRENCINO",
                    "OKULNO");

            var ogrenciNoEslesiyor =
                EslesiyorMu(
                    pdfOgrenciNo,
                    ogrenci.OgrenciNo);

            rapor.Add(
                ogrenciNoEslesiyor
                    ? "Öğrenci numarası bulundu ve eşleşiyor."
                    : $"Öğrenci numarası bulunamadı veya eşleşmiyor. " +
                      $"PDF: '{pdfOgrenciNo}' | " +
                      $"Veritabanı: '{ogrenci.OgrenciNo}'");

            belge.OgrenciBilgileriKontrolu =
                adSoyadEslesiyor &&
                ogrenciNoEslesiyor;

            // =====================================================
            // 8. TC KİMLİK NUMARASI
            // =====================================================

            var pdfTc =
                DegeriBul(
                    metin,
                    ogrenci.TcKimlikNo,
                    "TCKIMLIKNO",
                    "TCKIMLIKNUMARASI",
                    "TCKIMLIK");

            var tcEslesiyor =
                EslesiyorMu(
                    pdfTc,
                    ogrenci.TcKimlikNo);

            belge.TcKimlikKontrolu =
                tcEslesiyor;

            rapor.Add(
                tcEslesiyor
                    ? "TC Kimlik Numarası bulundu ve eşleşiyor."
                    : $"TC Kimlik Numarası bulunamadı veya eşleşmiyor. " +
                      $"PDF: '{pdfTc}' | " +
                      $"Veritabanı: '{ogrenci.TcKimlikNo}'");

            // =====================================================
            // 9. E-POSTA
            // =====================================================

            var pdfEposta =
                EpostaBul(
                    hamMetin,
                    metin,
                    ogrenci.Eposta);

            var epostaEslesiyor =
                EpostaEslesiyorMu(
                    pdfEposta,
                    ogrenci.Eposta);

            rapor.Add(
                epostaEslesiyor
                    ? "E-posta adresi bulundu ve eşleşiyor."
                    : $"E-posta bulunamadı veya eşleşmiyor. " +
                      $"PDF: '{pdfEposta}' | " +
                      $"Veritabanı: '{ogrenci.Eposta}'");

            // =====================================================
            // 10. TELEFON
            // =====================================================

            var pdfTelefon =
                TelefonBul(
                    hamMetin,
                    metin,
                    ogrenci.TelefonNo);

            var telefonEslesiyor =
                TelefonEslesiyorMu(
                    pdfTelefon,
                    ogrenci.TelefonNo);

            rapor.Add(
                telefonEslesiyor
                    ? "Telefon numarası bulundu ve eşleşiyor."
                    : $"Telefon numarası bulunamadı veya eşleşmiyor. " +
                      $"PDF: '{pdfTelefon}' | " +
                      $"Veritabanı: '{ogrenci.TelefonNo}'");

            // =====================================================
            // 11. İKAMETGÂH ADRESİ
            // =====================================================

            /*
             * PDF'nin gerçek yapısı:
             *
             * IKAMETGAHADRESI
             * SAĞLIKYARDIMSTATUSU
             *
             * Yani adres alanı boşsa iki etiket arasında
             * hiçbir veri bulunmuyor.
             *
             * Burada artık genel "ADRES" araması yapılmıyor.
             * Böylece SAĞLIKYARDIMSTATUSU metni adres olarak
             * alınmıyor.
             */

            var pdfAdres =
                AlanArasindanDegerBul(
                    metin,
                    "IKAMETGAHADRESI",
                    "SAGLIKYARDIMSTATUSU");

            var adresEslesiyor =
                EslesiyorMu(
                    pdfAdres,
                    ogrenci.IkametgahAdresi);

            rapor.Add(
                adresEslesiyor
                    ? "İkametgâh adresi bulundu ve eşleşiyor."
                    : string.IsNullOrWhiteSpace(pdfAdres)
                        ? "İkametgâh adresi PDF içinde boş."
                        : $"İkametgâh adresi bulunamadı veya eşleşmiyor. " +
                          $"PDF: '{pdfAdres}' | " +
                          $"Veritabanı: '{ogrenci.IkametgahAdresi}'");

            belge.IletisimBilgileriKontrolu =
                epostaEslesiyor &&
                telefonEslesiyor &&
                adresEslesiyor;

            // =====================================================
            // 12. STAJ BAŞLANGIÇ TARİHİ
            // =====================================================

            var pdfBaslangic =
                EtiketSonrasiRegex(
                    metin,
                    new[]
                    {
                        "STAJBASLAMATARIHI",
                        "STAJBASLANGICTARIHI",
                        "STAJBASLANGIC"
                    },
                    @"\d{1,2}[./]\d{1,2}[./]\d{4}");

            // =====================================================
            // 13. STAJ BİTİŞ TARİHİ
            // =====================================================

            var pdfBitis =
                EtiketSonrasiRegex(
                    metin,
                    new[]
                    {
                        "STAJBITISTARIHI",
                        "STAJBITIS"
                    },
                    @"\d{1,2}[./]\d{1,2}[./]\d{4}");

            var baslangicGecerli =
                TarihCevir(
                    pdfBaslangic,
                    out var baslangicTarihi);

            var bitisGecerli =
                TarihCevir(
                    pdfBitis,
                    out var bitisTarihi);

            var tarihlerGecerli =
                baslangicGecerli &&
                bitisGecerli &&
                baslangicTarihi <= bitisTarihi;

            belge.TarihKontrolu =
                tarihlerGecerli;

            rapor.Add(
                tarihlerGecerli
                    ? $"Staj tarihleri bulundu: " +
                      $"{pdfBaslangic} - {pdfBitis}"
                    : $"Staj tarihleri bulunamadı veya geçersiz. " +
                      $"Başlangıç: '{pdfBaslangic}', " +
                      $"Bitiş: '{pdfBitis}'");

            // =====================================================
            // 14. STAJ GÜN SAYISI
            // =====================================================

            /*
             * PDF'de gerçek metin:
             *
             * STAJGÜNSAYISI30
             *
             * Normalize edilmiş metinde:
             *
             * STAJGUNSAYISI30
             *
             * Bu nedenle etiketten hemen sonraki sayı alınır.
             */

            var pdfGunSayisi =
                StajGunSayisiBul(metin);

            var gunSayisiGecerli =
                int.TryParse(
                    pdfGunSayisi,
                    out var gunSayisi) &&
                gunSayisi > 0;

            belge.StajGunKontrolu =
                gunSayisiGecerli;

            rapor.Add(
                gunSayisiGecerli
                    ? $"Staj gün sayısı bulundu: {gunSayisi}"
                    : "Staj gün sayısı bulunamadı veya geçersiz.");

            // =====================================================
            // 15. İMZA
            // =====================================================

            belge.ImzaKontrolu = false;

            rapor.Add(
                "İmza alanı otomatik olarak doğrulanamadı. " +
                "Görsel kontrol gerekli.");

            // =====================================================
            // 16. KAŞE
            // =====================================================

            belge.KaseKontrolu = false;

            rapor.Add(
                "Kaşe alanı otomatik olarak doğrulanamadı. " +
                "Görsel kontrol gerekli.");

            // =====================================================
            // 17. BOŞ ALAN KONTROLÜ
            // =====================================================

            belge.BosAlanKontrolu =
                !string.IsNullOrWhiteSpace(pdfAdSoyad) &&
                !string.IsNullOrWhiteSpace(pdfOgrenciNo) &&
                !string.IsNullOrWhiteSpace(pdfTc) &&
                !string.IsNullOrWhiteSpace(pdfEposta) &&
                !string.IsNullOrWhiteSpace(pdfTelefon) &&
                !string.IsNullOrWhiteSpace(pdfAdres) &&
                tarihlerGecerli &&
                gunSayisiGecerli;

            /*
             * Burada adres PDF'de gerçekten boşsa,
             * zorunlu alan kontrolü başarısız olacaktır.
             *
             * Bu beklenen davranıştır.
             */

            rapor.Add(
                belge.BosAlanKontrolu
                    ? "Zorunlu metin alanları bulundu."
                    : "Zorunlu alanlardan en az biri boş veya okunamadı.");

            // =====================================================
            // 18. SON DURUM
            // =====================================================

            var otomatikKontrollerBasarili =
                belge.FormatKontrolu &&
                belge.OgrenciBilgileriKontrolu &&
                belge.TcKimlikKontrolu &&
                belge.IletisimBilgileriKontrolu &&
                belge.TarihKontrolu &&
                belge.StajGunKontrolu &&
                belge.BosAlanKontrolu;

            if (!otomatikKontrollerBasarili)
            {
                belge.Durum =
                    "Hatalı/Eksik";

                rapor.Add(
                    "Belgede hatalı veya eksik alanlar bulundu. " +
                    "Öğrencinin belgeyi düzeltmesi gerekiyor.");
            }
            else
            {
                // İmza ve kaşe otomatik olarak doğrulanamadığı için
                // burada doğrudan "Onaylandı" vermiyoruz.
                belge.Durum =
                    "İnceleniyor";

                rapor.Add(
                    "Otomatik metin kontrolleri başarılı. " +
                    "İmza ve kaşe için yönetici kontrolü bekleniyor.");
            }

            belge.KontrolRaporu =
                string.Join(
                    Environment.NewLine,
                    rapor);

            await _db.SaveChangesAsync();
        }

        // =========================================================
        // PDF METNİNİ OKU
        // =========================================================

        private static string PdfMetniniOku(
            string dosyaYolu)
        {
            var sb =
                new StringBuilder();

            using var document =
                PdfDocument.Open(dosyaYolu);

            foreach (var sayfa in document.GetPages())
            {
                sb.AppendLine(
                    sayfa.Text);
            }

            return sb.ToString();
        }

        // =========================================================
        // ARAMA METNİNE DÖNÜŞTÜR
        // =========================================================

        private static string AramaMetnineDonustur(
            string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
                return string.Empty;

            var sonuc =
                metin
                    .ToUpperInvariant()
                    .Replace('İ', 'I')
                    .Replace('ı', 'I')
                    .Replace('Ş', 'S')
                    .Replace('Ğ', 'G')
                    .Replace('Ü', 'U')
                    .Replace('Ö', 'O')
                    .Replace('Ç', 'C')
                    .Replace('Â', 'A');

            return Regex.Replace(
                sonuc,
                @"[^A-Z0-9./@_-]",
                "");
        }

        // =========================================================
        // GENEL DEĞER BUL
        // =========================================================

        private static string DegeriBul(
            string metin,
            string veritabaniDegeri,
            params string[] etiketler)
        {
            if (string.IsNullOrWhiteSpace(metin) ||
                string.IsNullOrWhiteSpace(veritabaniDegeri))
            {
                return string.Empty;
            }

            // Önce veritabanındaki gerçek değeri PDF'nin
            // herhangi bir yerinde arıyoruz.
            var dbDegeri =
                KarsilastirmaMetni(
                    veritabaniDegeri);

            if (!string.IsNullOrWhiteSpace(dbDegeri) &&
                metin.Contains(
                    dbDegeri,
                    StringComparison.OrdinalIgnoreCase))
            {
                return veritabaniDegeri;
            }

            // Bulunamazsa etiket sonrasındaki alanı arıyoruz.
            foreach (var etiket in etiketler)
            {
                var deger =
                    EtiketSonrasiDeger(
                        metin,
                        etiket,
                        etiketler);

                if (!string.IsNullOrWhiteSpace(deger))
                    return deger;
            }

            return string.Empty;
        }

        // =========================================================
        // İKİ ETİKET ARASINDAKİ ALANI BUL
        // =========================================================

        private static string AlanArasindanDegerBul(
            string metin,
            string baslangicEtiketi,
            string bitisEtiketi)
        {
            var baslangicIndex =
                metin.IndexOf(
                    baslangicEtiketi,
                    StringComparison.Ordinal);

            if (baslangicIndex < 0)
                return string.Empty;

            var veriBaslangici =
                baslangicIndex +
                baslangicEtiketi.Length;

            var bitisIndex =
                metin.IndexOf(
                    bitisEtiketi,
                    veriBaslangici,
                    StringComparison.Ordinal);

            if (bitisIndex < 0)
                return string.Empty;

            if (bitisIndex <= veriBaslangici)
                return string.Empty;

            return metin
                .Substring(
                    veriBaslangici,
                    bitisIndex - veriBaslangici)
                .Trim(
                    '.',
                    '/',
                    ' ',
                    '\r',
                    '\n',
                    ':',
                    '-');
        }

        // =========================================================
        // ETİKET SONRASI REGEX
        // =========================================================

        private static string EtiketSonrasiRegex(
            string metin,
            string[] etiketler,
            string degerPattern)
        {
            foreach (var etiket in etiketler)
            {
                var index =
                    metin.IndexOf(
                        etiket,
                        StringComparison.Ordinal);

                if (index < 0)
                    continue;

                var baslangic =
                    index + etiket.Length;

                var uzunluk =
                    Math.Min(
                        150,
                        metin.Length - baslangic);

                if (uzunluk <= 0)
                    continue;

                var parca =
                    metin.Substring(
                        baslangic,
                        uzunluk);

                var match =
                    Regex.Match(
                        parca,
                        degerPattern);

                if (match.Success)
                    return match.Value;
            }

            return string.Empty;
        }

        // =========================================================
        // ETİKET SONRASINDA DEĞER BUL
        // =========================================================

        private static string EtiketSonrasiDeger(
            string metin,
            string etiket,
            params string[] sonrakiEtiketler)
        {
            var index =
                metin.IndexOf(
                    etiket,
                    StringComparison.Ordinal);

            if (index < 0)
                return string.Empty;

            var baslangic =
                index + etiket.Length;

            var bitis =
                metin.Length;

            foreach (var sonraki in sonrakiEtiketler)
            {
                if (string.Equals(
                        sonraki,
                        etiket,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var sonrakiIndex =
                    metin.IndexOf(
                        sonraki,
                        baslangic,
                        StringComparison.Ordinal);

                if (sonrakiIndex >= 0 &&
                    sonrakiIndex < bitis)
                {
                    bitis =
                        sonrakiIndex;
                }
            }

            if (bitis <= baslangic)
                return string.Empty;

            return metin
                .Substring(
                    baslangic,
                    bitis - baslangic)
                .Trim(
                    '.',
                    '/',
                    ' ',
                    '\r',
                    '\n',
                    ':');
        }

        // =========================================================
        // E-POSTA BUL
        // =========================================================

        private static string EpostaBul(
            string hamMetin,
            string normalMetin,
            string dbEposta)
        {
            /*
             * PDF'deki gerçek yapı:
             *
             * E-POSTA ADRESİ/TELEFON NO
             * eliflklc.1012@gmail.com/05510785626
             *
             * Bu nedenle artık yalnızca DB'deki e-postayı
             * aramıyoruz. PDF içindeki gerçek e-posta adresini
             * regex ile buluyoruz.
             */

            var eslesmeler =
                Regex.Matches(
                    hamMetin,
                    @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
                    RegexOptions.IgnoreCase);

            foreach (Match match in eslesmeler)
            {
                if (match.Success)
                    return match.Value.Trim();
            }

            // PDF okuyucu bazı durumlarda boşluk ekleyebilir.
            var temiz =
                Regex.Replace(
                    hamMetin,
                    @"\s+",
                    "");

            var temizEslesme =
                Regex.Match(
                    temiz,
                    @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
                    RegexOptions.IgnoreCase);

            if (temizEslesme.Success)
                return temizEslesme.Value;

            return string.Empty;
        }

        // =========================================================
        // TELEFON BUL
        // =========================================================

        private static string TelefonBul(
            string hamMetin,
            string normalMetin,
            string dbTelefon)
        {
            /*
             * PDF'deki öğrenci telefonu:
             *
             * EPOSTAADRESITELEFONNO
             * eliflklc.1012@gmail.com/05510785626
             *
             * Daha sonra ayrıca:
             *
             * STAJYAPILANYERIN...
             * ...
             * TELEFONNO
             *
             * bulunduğu için etikete körü körüne bakmak yerine
             * telefon formatındaki tüm adayları buluyoruz.
             */

            var telefonAdaylari =
                Regex.Matches(
                    normalMetin,
                    @"(?<!\d)0?5\d{9,10}(?!\d)");

            foreach (Match match in telefonAdaylari)
            {
                if (match.Success)
                {
                    var aday =
                        Regex.Replace(
                            match.Value,
                            @"\D",
                            "");

                    return aday;
                }
            }

            return string.Empty;
        }

        // =========================================================
        // STAJ GÜN SAYISI BUL
        // =========================================================

        private static string StajGunSayisiBul(
            string metin)
        {
            /*
             * PDF'deki gerçek metin:
             *
             * STAJGÜNSAYISI30
             *
             * Normalize edilmiş metin:
             *
             * STAJGUNSAYISI30
             *
             * Dolayısıyla etiketten hemen sonra gelen
             * 1-3 basamaklı sayı alınır.
             */

            var etiketler =
                new[]
                {
                    "STAJGUNSAYISI",
                    "STAJGUN",
                    "TOPLAMSTAJGUNSAYISI",
                    "TOPLAMSTAJGUN",
                    "STAJYAPILANGUNSAYISI",
                    "STAJYAPILACAKGUNSAYISI",
                    "STAJSURESI"
                };

            foreach (var etiket in etiketler)
            {
                var index =
                    metin.IndexOf(
                        etiket,
                        StringComparison.Ordinal);

                if (index < 0)
                    continue;

                var baslangic =
                    index + etiket.Length;

                var uzunluk =
                    Math.Min(
                        20,
                        metin.Length - baslangic);

                if (uzunluk <= 0)
                    continue;

                var parca =
                    metin.Substring(
                        baslangic,
                        uzunluk);

                var match =
                    Regex.Match(
                        parca,
                        @"^\d{1,3}");

                if (match.Success)
                    return match.Value;
            }

            return string.Empty;
        }

        // =========================================================
        // GENEL KARŞILAŞTIRMA
        // =========================================================

        private static bool EslesiyorMu(
            string pdfDegeri,
            string veritabaniDegeri)
        {
            if (string.IsNullOrWhiteSpace(
                    pdfDegeri) ||
                string.IsNullOrWhiteSpace(
                    veritabaniDegeri))
            {
                return false;
            }

            return KarsilastirmaMetni(
                       pdfDegeri) ==
                   KarsilastirmaMetni(
                       veritabaniDegeri);
        }

        private static string KarsilastirmaMetni(
            string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
                return string.Empty;

            var sonuc =
                metin
                    .ToUpperInvariant()
                    .Replace("İ", "I")
                    .Replace("ı", "I")
                    .Replace("Ş", "S")
                    .Replace("Ğ", "G")
                    .Replace("Ü", "U")
                    .Replace("Ö", "O")
                    .Replace("Ç", "C")
                    .Replace("Â", "A");

            return Regex.Replace(
                sonuc,
                @"[^A-Z0-9]",
                "");
        }

        // =========================================================
        // E-POSTA KARŞILAŞTIRMA
        // =========================================================

        private static bool EpostaEslesiyorMu(
            string pdfEposta,
            string dbEposta)
        {
            if (string.IsNullOrWhiteSpace(
                    pdfEposta) ||
                string.IsNullOrWhiteSpace(
                    dbEposta))
            {
                return false;
            }

            var pdf =
                Regex.Replace(
                    pdfEposta,
                    @"\s+",
                    "");

            var db =
                Regex.Replace(
                    dbEposta,
                    @"\s+",
                    "");

            return string.Equals(
                pdf,
                db,
                StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // TELEFON KARŞILAŞTIRMA
        // =========================================================

        private static bool TelefonEslesiyorMu(
            string? pdfTelefon,
            string? dbTelefon)
        {
            var pdf =
                Regex.Replace(
                    pdfTelefon ?? string.Empty,
                    @"\D",
                    "");

            var db =
                Regex.Replace(
                    dbTelefon ?? string.Empty,
                    @"\D",
                    "");

            if (pdf.Length == 0 ||
                db.Length == 0)
            {
                return false;
            }

            // Başındaki 0 farkını tolere ediyoruz.
            return pdf.TrimStart('0') ==
                   db.TrimStart('0');
        }

        // =========================================================
        // TARİH ÇEVİR
        // =========================================================

        private static bool TarihCevir(
            string tarih,
            out DateTime sonuc)
        {
            return DateTime.TryParseExact(
                tarih,
                new[]
                {
                    "d.M.yyyy",
                    "dd.MM.yyyy",
                    "d/M/yyyy",
                    "dd/MM/yyyy",
                    "d-MM-yyyy",
                    "dd-MM-yyyy"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out sonuc);
        }

        // =========================================================
        // FORMAT KONTROLÜ
        // =========================================================

        private static bool FormatKontrol(
            string dosyaTuru)
        {
            return IzinliUzantilar.Contains(
                dosyaTuru,
                StringComparer.OrdinalIgnoreCase);
        }
    }
}