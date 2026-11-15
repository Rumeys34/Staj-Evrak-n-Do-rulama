using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StajEvrakDogrulama.Migrations
{
    /// <inheritdoc />
    public partial class Ek16KontrolAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BosAlanKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FormatKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IletisimBilgileriKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ImzaKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "KaseKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KontrolRaporu",
                table: "Ek16Belgeler",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "OgrenciBilgileriKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StajGunKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TarihKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TcKimlikKontrolu",
                table: "Ek16Belgeler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BosAlanKontrolu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "FormatKontrolu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "IletisimBilgileriKontrolu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "ImzaKontrolu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "KaseKontrolu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "KontrolRaporu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "OgrenciBilgileriKontrolu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "StajGunKontrolu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "TarihKontrolu",
                table: "Ek16Belgeler");

            migrationBuilder.DropColumn(
                name: "TcKimlikKontrolu",
                table: "Ek16Belgeler");
        }
    }
}
