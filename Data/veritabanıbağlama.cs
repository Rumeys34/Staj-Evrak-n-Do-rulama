
using Microsoft.EntityFrameworkCore;
using StajEvrakDogrulama.Models;

namespace StajEvrakDogrulama.Data
{
    public class VeritabaniBaglami : DbContext
    {
        public VeritabaniBaglami(
            DbContextOptions<VeritabaniBaglami> options)
            : base(options)
        {
        }

        public DbSet<User> Kullanicilar { get; set; }

        public DbSet<Ogrenci> Ogrenciler { get; set; }

        public DbSet<Yonetici> Yoneticiler { get; set; }
    }
}
