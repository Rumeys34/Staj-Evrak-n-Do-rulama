namespace StajEvrakDogrulama.Models;

public sealed class AdminDashboardViewModel
{
    public IReadOnlyList<Ogrenci> Students { get; init; } = [];
}
