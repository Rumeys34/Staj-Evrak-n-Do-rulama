namespace StajEvrakDogrulama.Models;

public sealed class StudentImportRow
{
    public int RowNumber { get; init; }
    public string AdSoyad { get; init; } = string.Empty;
    public string KullaniciAdi { get; init; } = string.Empty;
    public string Eposta { get; init; } = string.Empty;
    public string TcKimlikNo { get; init; } = string.Empty;
    public string OgrenciNo { get; init; } = string.Empty;
    public string Bolum { get; init; } = string.Empty;
    public string TelefonNo { get; init; } = string.Empty;
    public string IkametgahAdresi { get; init; } = string.Empty;
    public DateTime? DogumTarihi { get; init; }
    public string TeslimYili { get; init; } = string.Empty;
    public int OncekiStajGunSayisi { get; init; }
    public string Uyruk { get; init; } = string.Empty;
    public string SaglikYardimiDurumu { get; init; } = string.Empty;
}
