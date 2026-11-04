namespace SeconSbtiBackend.DTOs;

public sealed class PurchasedServiceResponse
{
    public Guid ReportId { get; init; }
    public string AccountNo { get; init; } = string.Empty;
    public string? ServiceLabel { get; init; }
    public Guid? ServiceCategoryId { get; init; }
    public string? ServiceCategoryName { get; init; }
    public DateOnly PeriodFrom { get; init; }
    public DateOnly PeriodTo { get; init; }
    public decimal Value { get; init; }
    public string Unit { get; init; } = "sek";
    public string MeasurementQuality { get; init; } = string.Empty;
    public string? Comment { get; init; }
}