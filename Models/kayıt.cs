
using System.ComponentModel.DataAnnotations;

namespace StajEvrakDogrulama.Models
{
    public class Kayit
    {
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        public string AdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
        public string Eposta { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
        [DataType(DataType.Password)]
        public string SifreTekrar { get; set; } = string.Empty;
    }
}
