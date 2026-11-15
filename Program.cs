using StajEvrakDogrulama.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Data;
using StajEvrakDogrulama.Models;
using StajEvrakDogrulama.Yardimcilar;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VeritabaniBaglami>(options =>
    options.UseSqlite("Data Source=StajEvrakDogrulama.db"));

builder.Services.AddControllersWithViews();

// Öğrenci veri seti içe aktarma servisleri
builder.Services.AddScoped<IStudentImportParser, CsvStudentImportParser>();
builder.Services.AddScoped<IStudentImportParser, ExcelStudentImportParser>();
builder.Services.AddScoped<StudentImportService>();

// Ek-16 kontrol servisi
builder.Services.AddScoped<Ek16KontrolService>();

// Kimlik doğrulama
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Giris/Index";
        options.AccessDeniedPath = "/Giris/Index";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


// ---------------------------------------------------------
// SİSTEM BAŞLANGIÇ İŞLEMLERİ
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<VeritabaniBaglami>();

    // Admin hesabı yoksa oluştur.
    // Öğrenci bilgileri burada oluşturulmaz veya değiştirilmez.
    var adminExists = await db.Kullanicilar
        .AnyAsync(x => x.KullaniciAdi == "admin");

    if (!adminExists)
    {
        var admin = new User
        {
            AdSoyad = "Sistem Yöneticisi",
            KullaniciAdi = "admin",
            Eposta = "admin@test.com",
            SifreHash = SifreYardimcisi.SifreyiHashle("Admin123!"),
            Rol = "ROLE_ADMIN"
        };

        db.Kullanicilar.Add(admin);

        await db.SaveChangesAsync();
    }
}

app.Run();