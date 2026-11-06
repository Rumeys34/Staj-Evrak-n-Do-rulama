namespace StajEvrakDogrulama.Models
{
    public class Ogrenci : User
    {
        public string TcKimlikNo { get; set; } = string.Empty;

        public DateTime DogumTarihi { get; set; }

        public string IkametgahAdresi { get; set; } = string.Empty;

        public string OgrenciNo { get; set; } = string.Empty;

        public string Bolum { get; set; } = string.Empty;

        public string Uyruk { get; set; } = string.Empty;

        public string TeslimYili { get; set; } = string.Empty;

        public int OncekiStajGunSayisi { get; set; }

        public string TelefonNo { get; set; } = string.Empty;

        public string SaglikYardimiDurumu { get; set; } = string.Empty;
    }
}