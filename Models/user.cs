
namespace StajEvrakDogrulama.Models
{
    public class User
    {
        public int Id { get; set; }

        public string AdSoyad { get; set; } = string.Empty;

        public string KullaniciAdi { get; set; } = string.Empty;

        public string Eposta { get; set; } = string.Empty;

        public string SifreHash { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;
    }
}
