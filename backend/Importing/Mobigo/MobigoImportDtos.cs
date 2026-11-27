namespace SeconSbtiBackend.Importing.Mobigo;

/// <summary>
/// Result of a preview or an import of a Mobigo export. The same body for both, so the frontend
/// shows a preview and the saved import the same way.
/// </summary>
public record MobigoImportResponse
{
    public required string FileName { get; init; }

    /// <summary>Earliest and latest delivery date of the lines to import. Null if there are none.</summary>
    public DateOnly? DeliveryDateFrom { get; init; }
    public DateOnly? DeliveryDateTo { get; init; }

    /// <summary>Sum of the Summa column.</summary>
    public decimal Total { get; init; }

    /// <summary>The SUMMA: row at the bottom of the file. Null if it is missing.</summary>
    public decimal? FileTotal { get; init; }

    public bool CanImport { get; init; }
    public required List<string> Blockers { get; init; }
    public required List<string> Warnings { get; init; }
    public required List<MobigoIssueResponse> Issues { get; init; }
    public MobigoPreviousImportResponse? PreviousImport { get; init; }

    public required MobigoLinesSummary ToImport { get; init; }
    public required MobigoLinesSummary AlreadyImported { get; init; }

    /// <summary>
    /// The lines that are saved. Rows with the same work order, article and delivery date are one line.
    /// </summary>
    public required List<MobigoLineResponse> LinesToImport { get; init; }

    /// <summary>
    /// Lines imported before with another quantity than in this file. They are not changed.
    /// </summary>
    public required List<MobigoChangedLineResponse> ChangedLines { get; init; }

    public required List<MobigoNewProductResponse> NewProducts { get; init; }
    public required List<MobigoSkippedRowResponse> SkippedRows { get; init; }

    public Guid? ImportBatchId { get; init; }
}

public record MobigoIssueResponse(int RowNumber, string Code);

public record MobigoPreviousImportResponse(DateTime ImportedAt, string ImportedBy);

/// <param name="Lines">Lines after rows with the same work order, article and date are merged.</param>
/// <param name="Rows">Rows in the file.</param>
public record MobigoLinesSummary(int Lines, int Rows, decimal Quantity);

public record MobigoLineResponse(
    string WorkOrder,
    string ArticleNumber,
    string ProductName,
    bool IsNewProduct,
    DateOnly DeliveryDate,
    decimal Quantity,
    List<int> RowNumbers);

public record MobigoChangedLineResponse(
    string WorkOrder,
    string ArticleNumber,
    string ProductName,
    DateOnly DeliveryDate,
    decimal Quantity,
    decimal ImportedQuantity,
    List<int> RowNumbers);

public record MobigoNewProductResponse(string ArticleNumber, string Name);

public record MobigoSkippedRowResponse(
    int RowNumber,
    string Reason,
    string WorkOrder,
    string? Status,
    string? ArticleNumber,
    string? Name,
    DateOnly? DeliveryDate,
    decimal? Quantity,
    string? Unit);

public static class MobigoBlockers
{
    public const string FileAlreadyImported = "FileAlreadyImported";
    public const string ReconciliationFailed = "ReconciliationFailed";
    public const string InvalidRows = "InvalidRows";
    public const string NothingToImport = "NothingToImport";
}

public static class MobigoWarnings
{
    public const string RowsSkipped = "RowsSkipped";
    public const string ChangedSinceImport = "ChangedSinceImport";
}

/// <summary>
/// Issues found by the import, in addition to <see cref="ParsedMobigoIssueCodes"/> found by the parser.
/// They are only checked on the rows that would be saved.
/// </summary>
public static class MobigoIssueCodes
{
    public const string WorkOrderMissing = "WorkOrderMissing";
    public const string DeliveryDateMissing = "DeliveryDateMissing";
    public const string QuantityMissing = "QuantityMissing";
    public const string QuantityNegative = "QuantityNegative";
}
