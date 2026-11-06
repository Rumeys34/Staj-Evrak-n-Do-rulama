using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StajEvrakDogrulama.Migrations
{
    /// <inheritdoc />
    public partial class OgrenciBilgileriniGuncelle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DogumTarihi",
                table: "Kullanicilar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IkametgahAdresi",
                table: "Kullanicilar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaglikYardimiDurumu",
                table: "Kullanicilar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TcKimlikNo",
                table: "Kullanicilar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonNo",
                table: "Kullanicilar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeslimYili",
                table: "Kullanicilar",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DogumTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "IkametgahAdresi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SaglikYardimiDurumu",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "TcKimlikNo",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "TelefonNo",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "TeslimYili",
                table: "Kullanicilar");
        }
    }
}
