namespace StajEvrakDogrulama.Models;

public sealed class StudentImportResult
{
    private readonly List<string> _errors = [];
    private readonly List<ImportedStudentAccount> _accounts = [];

    public int TotalRows { get; private set; }
    public int ValidRows { get; private set; }

    public IReadOnlyList<string> Errors => _errors;
    public IReadOnlyList<ImportedStudentAccount> Accounts => _accounts;

    public bool IsValid => TotalRows > 0 && _errors.Count == 0;

    internal void SetTotalRows(int count)
    {
        TotalRows = count;
    }

    internal void AddValidRow()
    {
        ValidRows++;
    }

    internal void AddError(string message)
    {
        _errors.Add(message);
    }

    internal void AddAccount(ImportedStudentAccount account)
    {
        _accounts.Add(account);
    }
}

public sealed record ImportedStudentAccount(
    string OgrenciNo,
    string KullaniciAdi,
    string TemporaryPassword);