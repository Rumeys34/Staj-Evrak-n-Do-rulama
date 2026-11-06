
using System.ComponentModel.DataAnnotations;

namespace StajEvrakDogrulama.Models
{
    public class Login
    {
        [Required(ErrorMessage = "Kullanıcı adı veya e-posta zorunludur.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = string.Empty;
    }
}
