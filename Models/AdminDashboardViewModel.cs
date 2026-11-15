namespace StajEvrakDogrulama.Models;

public sealed class AdminDashboardViewModel
{
    public IReadOnlyList<Ogrenci> Students { get; init; } = [];
    public IReadOnlyList<Ek16Belge> Belgeler { get; init; } = [];
}
