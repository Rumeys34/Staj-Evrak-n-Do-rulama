using StajEvrakDogrulama.Models;

namespace StajEvrakDogrulama.Services;

public interface IStudentImportParser
{
    bool CanParse(string extension);

    IReadOnlyList<StudentImportRow> Parse(Stream stream);
}