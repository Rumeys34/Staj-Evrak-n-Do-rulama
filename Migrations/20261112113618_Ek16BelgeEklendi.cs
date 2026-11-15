using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StajEvrakDogrulama.Migrations
{
    /// <inheritdoc />
    public partial class Ek16BelgeEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ek16Belgeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OgrenciId = table.Column<int>(type: "INTEGER", nullable: false),
                    DosyaAdi = table.Column<string>(type: "TEXT", nullable: false),
                    DosyaYolu = table.Column<string>(type: "TEXT", nullable: false),
                    DosyaTuru = table.Column<string>(type: "TEXT", nullable: false),
                    YuklenmeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Durum = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ek16Belgeler", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ek16Belgeler");
        }
    }
}
