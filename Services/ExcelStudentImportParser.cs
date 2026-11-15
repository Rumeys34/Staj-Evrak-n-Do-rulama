using System.Globalization;
using ClosedXML.Excel;
using StajEvrakDogrulama.Models;

namespace StajEvrakDogrulama.Services;

public sealed class ExcelStudentImportParser : IStudentImportParser
{
    public bool CanParse(string extension)
    {
        return extension.Equals(
            ".xlsx",
            StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<StudentImportRow> Parse(Stream stream)
    {
        using var workbook =
            new XLWorkbook(stream);

        var worksheet =
            workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            throw new InvalidDataException(
                "Excel dosyasında çalışma sayfası bulunamadı.");
        }

        var headerCells =
            worksheet
                .Row(1)
                .CellsUsed()
                .ToDictionary(
                    cell => cell.Address.ColumnNumber,
                    cell => cell.GetString().Trim());

        var students =
            new List<StudentImportRow>();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            string GetValue(string columnName)
            {
                var header =
                    headerCells.FirstOrDefault(
                        item => item.Value.Equals(
                            columnName,
                            StringComparison.OrdinalIgnoreCase));

                if (header.Key == 0)
                {
                    return string.Empty;
                }

                return row
                    .Cell(header.Key)
                    .GetFormattedString()
                    .Trim();
            }

            DateTime? birthDate = null;

            var birthDateCell =
                GetValue("DogumTarihi");

            if (DateTime.TryParse(
                    birthDateCell,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                birthDate = parsedDate;
            }

            var internshipDaysText =
                GetValue("OncekiStajGunSayisi");

            int internshipDays = 0;

            if (!int.TryParse(
                    internshipDaysText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out internshipDays))
            {
                int.TryParse(
                    internshipDaysText,
                    NumberStyles.Integer,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    out internshipDays);
            }

            students.Add(
                new StudentImportRow
                {
                    RowNumber =
                        row.RowNumber(),

                    AdSoyad =
                        GetValue("AdSoyad"),

                    KullaniciAdi =
                        GetValue("KullaniciAdi"),

                    Eposta =
                        GetValue("Eposta"),

                    TcKimlikNo =
                        GetValue("TcKimlikNo"),

                    OgrenciNo =
                        GetValue("OgrenciNo"),

                    Bolum =
                        GetValue("Bolum"),

                    TelefonNo =
                        GetValue("TelefonNo"),

                    IkametgahAdresi =
                        GetValue("IkametgahAdresi"),

                    DogumTarihi =
                        birthDate,

                    TeslimYili =
                        GetValue("TeslimYili"),

                    OncekiStajGunSayisi =
                        internshipDays,

                    Uyruk =
                        GetValue("Uyruk"),

                    SaglikYardimiDurumu =
                        GetValue("SaglikYardimiDurumu")
                });
        }

        return students;
    }
}