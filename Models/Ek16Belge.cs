using System.ComponentModel.DataAnnotations;

namespace StajEvrakDogrulama.Models
{
    public class Ek16Belge
    {
        // Belge durumları
        public const string DurumKontrolBekliyor = "Kontrol Bekliyor";
        public const string DurumInceleniyor = "İnceleniyor";
        public const string DurumHataliEksik = "Hatalı/Eksik";
        public const string DurumOnaylandi = "Onaylandı";
        public const string DurumTamamlandi = "Tamamlandı";

        // Belge kimliği
        public int Id { get; set; }

        // Belgenin hangi öğrenciye ait olduğu
        public int OgrenciId { get; set; }

        // Dosya bilgileri
        [Required]
        public string DosyaAdi { get; set; } = string.Empty;

        [Required]
        public string DosyaYolu { get; set; } = string.Empty;

        [Required]
        public string DosyaTuru { get; set; } = string.Empty;

        // Yüklenme tarihi
        public DateTime YuklenmeTarihi { get; set; } = DateTime.Now;

        // Belgenin mevcut durumu
        public string Durum { get; set; } = DurumKontrolBekliyor;

        // Kontrol sonuçları

        // PDF, Word veya resim uzantısı kontrolü
        public bool FormatKontrolu { get; set; }

        // Ad Soyad ve öğrenci numarası kontrolü
        public bool OgrenciBilgileriKontrolu { get; set; }

        // TC kimlik numarası kontrolü
        public bool TcKimlikKontrolu { get; set; }

        // Telefon, e-posta ve adres kontrolü
        public bool IletisimBilgileriKontrolu { get; set; }

        // Staj tarihleri kontrolü
        public bool TarihKontrolu { get; set; }

        // Staj gün sayısı kontrolü
        public bool StajGunKontrolu { get; set; }

        // İmza kontrolü
        public bool ImzaKontrolu { get; set; }

        // Kaşe kontrolü
        public bool KaseKontrolu { get; set; }

        // Zorunlu alanların boş olup olmadığı
        public bool BosAlanKontrolu { get; set; }

        // Öğrenciye gösterilecek kontrol raporu
        public string KontrolRaporu { get; set; } = string.Empty;
    }
}