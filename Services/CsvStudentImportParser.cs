using System.Globalization;
using StajEvrakDogrulama.Models;

namespace StajEvrakDogrulama.Services;

public sealed class CsvStudentImportParser : IStudentImportParser
{
    public bool CanParse(string extension)
    {
        return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<StudentImportRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(
            stream,
            System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        var lines = reader.ReadToEnd()
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
            return [];

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

                return index >= 0 && index < values.Count
                    ? values[index].Trim()
                    : string.Empty;
            }

            DateTime? birthDate = DateTime.TryParse(
                GetValue("DogumTarihi"),
                CultureInfo.GetCultureInfo("tr-TR"),
                DateTimeStyles.None,
                out var parsedDate)
                    ? parsedDate
                    : null;

            int.TryParse(GetValue("OncekiStajGunSayisi"), out var internshipDays);

            students.Add(new StudentImportRow
            {
                RowNumber = i + 1,
                AdSoyad = GetValue("AdSoyad"),
                KullaniciAdi = GetValue("KullaniciAdi"),
                Eposta = GetValue("Eposta"),
                TcKimlikNo = GetValue("TcKimlikNo"),
                OgrenciNo = GetValue("OgrenciNo"),
                Bolum = GetValue("Bolum"),
                TelefonNo = GetValue("TelefonNo"),
                IkametgahAdresi = GetValue("IkametgahAdresi"),
                DogumTarihi = birthDate,
                TeslimYili = GetValue("TeslimYili"),
                OncekiStajGunSayisi = internshipDays,
                Uyruk = GetValue("Uyruk"),
                SaglikYardimiDurumu = GetValue("SaglikYardimiDurumu")
            });
        }

        return students;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        var insideQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];

            if (character == '"' && insideQuotes &&
                i + 1 < line.Length && line[i + 1] == '"')
            {
                currentField.Append('"');
                i++;
            }
            else if (character == '"')
            {
                insideQuotes = !insideQuotes;
            }
            else if (character == ',' && !insideQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(character);
            }
        }

        fields.Add(currentField.ToString());
        return fields;
    }
}