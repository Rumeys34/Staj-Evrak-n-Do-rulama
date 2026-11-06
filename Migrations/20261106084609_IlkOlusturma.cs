using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StajEvrakDogrulama.Migrations
{
    /// <inheritdoc />
    public partial class IlkOlusturma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdSoyad = table.Column<string>(type: "TEXT", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "TEXT", nullable: false),
                    Eposta = table.Column<string>(type: "TEXT", nullable: false),
                    SifreHash = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    OgrenciNo = table.Column<string>(type: "TEXT", nullable: true),
                    Bolum = table.Column<string>(type: "TEXT", nullable: true),
                    Uyruk = table.Column<string>(type: "TEXT", nullable: true),
                    OncekiStajGunSayisi = table.Column<int>(type: "INTEGER", nullable: true),
                    YoneticiUnvani = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}
