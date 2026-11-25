namespace SeconSbtiBackend.Importing.Mobigo;

public class ParsedMobigoFile
{
    public List<ParsedMobigoLine> Lines { get; } = [];

    /// <summary>
    /// From the SUMMA: row at the bottom. Null if the row is missing.
    /// </summary>
    public decimal? Total { get; set; }

    public int? TotalRowNumber { get; set; }

    public List<ParsedMobigoIssue> Issues { get; } = [];
}

/// <summary>
/// One row of the export, as it is in the file.
/// </summary>
public class ParsedMobigoLine
{
    public required int RowNumber { get; init; }

    /// <summary>Uppdrag, e.g. U012460 or AVT-0068-0015.</summary>
    public required string WorkOrder { get; init; }

    /// <summary>Task, e.g. Arkiverat, Klart, Pågående.</summary>
    public string? Status { get; init; }

    public string? Customer { get; init; }

    /// <summary>Nr. Null for rows that are not an article, e.g. work, rent or travel.</summary>
    public string? ArticleNumber { get; init; }

    /// <summary>Namn.</summary>
    public string? Name { get; init; }

    public DateOnly? DeliveryDate { get; init; }

    /// <summary>Antal. Null if the cell is empty.</summary>
    public decimal? Quantity { get; init; }

    /// <summary>Enhet, e.g. st.</summary>
    public string? Unit { get; init; }

    public bool Invoiced { get; init; }

    public decimal? UnitPrice { get; init; }

    /// <summary>A fraction, e.g. -0.1 for 10 % off.</summary>
    public decimal? Discount { get; init; }

    /// <summary>Summa. Empty for rows that are not invoiced.</summary>
    public decimal? Amount { get; init; }
}

public record ParsedMobigoIssue(int RowNumber, string Code);

public static class ParsedMobigoIssueCodes
{
    public const string TotalMissing = "TotalMissing";
    public const string TotalMismatch = "TotalMismatch";
    public const string AmountMismatch = "AmountMismatch";
}
