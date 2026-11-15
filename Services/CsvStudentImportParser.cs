using System.Globalization;
using System.Text;
using StajEvrakDogrulama.Models;

namespace StajEvrakDogrulama.Services;

public sealed class CsvStudentImportParser : IStudentImportParser
{
    public bool CanParse(string extension)
    {
        return extension.Equals(
            ".csv",
            StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<StudentImportRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        var content = reader.ReadToEnd();

        var lines = content
            .Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            return [];
        }

        var headers = SplitCsvLine(lines[0])
            .Select(header => header.Trim())
            .ToList();

        var students = new List<StudentImportRow>();

        for (var i = 1; i < lines.Length; i++)
        {
            var values = SplitCsvLine(lines[i]);

            string GetValue(string columnName)
            {
                var index = headers.FindIndex(
                    header => header.Equals(
                        columnName,
                        StringComparison.OrdinalIgnoreCase));

                if (index < 0 || index >= values.Count)
                {
                    return string.Empty;
                }

                return values[index].Trim();
            }

            var dogumTarihiText =
                GetValue("DogumTarihi");

            DateTime? birthDate = null;

            if (DateTime.TryParse(
                    dogumTarihiText,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                birthDate = parsedDate;
            }

            var stajGunText =
                GetValue("OncekiStajGunSayisi");

            int internshipDays = 0;

            if (!int.TryParse(
                    stajGunText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out internshipDays))
            {
                int.TryParse(
                    stajGunText,
                    NumberStyles.Integer,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    out internshipDays);
            }

            students.Add(
                new StudentImportRow
                {
                    RowNumber = i + 1,

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

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();

        var currentField =
            new StringBuilder();

        var insideQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];

            if (character == '"' &&
                insideQuotes &&
                i + 1 < line.Length &&
                line[i + 1] == '"')
            {
                currentField.Append('"');
                i++;

                continue;
            }

            if (character == '"')
            {
                insideQuotes = !insideQuotes;

                continue;
            }

            if (character == ',' &&
                !insideQuotes)
            {
                fields.Add(
                    currentField.ToString());

                currentField.Clear();

                continue;
            }

            currentField.Append(character);
        }

        fields.Add(
            currentField.ToString());

        return fields;
    }
}