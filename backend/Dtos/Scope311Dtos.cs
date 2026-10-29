namespace SeconSbtiBackend.Dtos;

/// <summary>
/// One sold product in the scope 3.11 report (FK-S.05).
/// </summary>
public record Scope311ProductRow(
    Guid ProductId,
    string? ArticleNo,
    string Name,
    Guid? GroupId,
    string? GroupName,
    decimal Quantity,
    decimal? KwhPerYear,
    decimal? LifetimeYears,
    string MeasurementQuality,
    string? Comment,
    decimal? ActivityKwh,
    IReadOnlyList<string> Missing);


/// <summary>
/// Totals for one product group. Products without a group end up in "Ingen grupp".
/// </summary>
public record Scope311GroupSummary(
    Guid? GroupId,
    string GroupName,
    decimal ActivityKwh,
    decimal SharePercent,
    int ProductCount,
    int IncompleteCount);


/// <summary>
/// Response for GET /api/scope311: totals per group and one row per product sold that year.
/// </summary>
public record Scope311Response(
    int Year,
    decimal TotalActivityKwh,
    IReadOnlyList<Scope311GroupSummary> Groups,
    IReadOnlyList<Scope311ProductRow> Products);