namespace SeconSbtiBackend.Importing.Visma4010;

public record Visma4010ImportResponse
{
    public required string FileName { get; init; }
    public string? Company { get; init; }
    public DateOnly? PeriodFrom { get; init; }
    public DateOnly? PeriodTo { get; init; }
    public bool IsPreliminary { get; init; }
    public required List<Visma4010AccountResponse> Accounts { get; init; }
    public bool CanImport { get; init; }
    public required List<string> Blockers { get; init; }
    public required List<string> Warnings { get; init; }
    public required List<Visma4010IssueResponse> Issues { get; init; }
    public Visma4010PreviousImportResponse? PreviousImport { get; init; }
    public required Visma4010RowsSummary ToImport { get; init; }
    public required Visma4010RowsSummary AlreadyImported { get; init; }
    public required Visma4010RowsSummary WithoutSupplier { get; init; }
    public required List<string> NewSuppliers { get; init; }
    public required List<Visma4010RowResponse> RowsWithoutSupplier { get; init; }
    public Guid? ImportBatchId { get; init; }
}

public record Visma4010AccountResponse(string Number, string Name);

public record Visma4010IssueResponse(int RowNumber, string Code);

public record Visma4010PreviousImportResponse(DateTime ImportedAt, string ImportedBy);

public record Visma4010RowsSummary(int Count, decimal NetSek);

public record Visma4010RowResponse(int RowNumber, string Account, string Voucher, DateOnly Date, string Text, decimal NetSek);

public static class Visma4010Blockers
{
    public const string FileAlreadyImported = "FileAlreadyImported";
    public const string ReconciliationFailed = "ReconciliationFailed";
    public const string DuplicateInvoiceNumber = "DuplicateInvoiceNumber";
    public const string NothingToImport = "NothingToImport";
}

public static class Visma4010Warnings
{
    public const string PreliminaryReport = "PreliminaryReport";
    public const string RowsWithoutSupplier = "RowsWithoutSupplier";
}
