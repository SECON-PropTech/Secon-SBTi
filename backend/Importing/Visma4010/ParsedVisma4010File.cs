namespace SeconSbtiBackend.Importing.Visma4010;

public class ParsedVisma4010File
{
    public string? Company { get; set; }
    public bool IsPreliminary { get; set; }
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set; }
    public List<ParsedAccount> Accounts { get; } = [];
    public List<ParsedIssue> Issues { get; } = [];
}

public class ParsedAccount
{
    public required int RowNumber { get; init; }
    public required string Number { get; init; }
    public required string Name { get; init; }
    public decimal OpeningBalance { get; set; }
    public ParsedSummaryRow? Turnover { get; set; }
    public ParsedSummaryRow? Closing { get; set; }
    public List<ParsedTransaction> Transactions { get; } = [];
}

public record ParsedSummaryRow(int RowNumber, decimal Debit, decimal Credit, decimal Balance);

public class ParsedTransaction
{
    public required int RowNumber { get; init; }
    public required string Account { get; init; }
    public required string Voucher { get; init; }
    public required DateOnly Date { get; init; }
    public required string Text { get; init; }
    public required decimal Debit { get; init; }
    public required decimal Credit { get; init; }
    public required decimal Balance { get; init; }
    public string? Supplier { get; set; }
    public string? InvoiceNumber { get; set; }

    public decimal Net => Debit - Credit;
}

public record ParsedIssue(int RowNumber, string Code);

public static class ParsedIssueCodes
{
    public const string BalanceMismatch = "BalanceMismatch";
    public const string TurnoverMissing = "TurnoverMissing";
    public const string TurnoverMismatch = "TurnoverMismatch";
    public const string ClosingBalanceMissing = "ClosingBalanceMissing";
    public const string ClosingBalanceMismatch = "ClosingBalanceMismatch";
    public const string DuplicateInvoiceNumber = "DuplicateInvoiceNumber";
}
