namespace SeconSbtiBackend.DTOs;

public class CreatePurchasedGoodRequest
{
    public required Guid SupplierId { get; init; }
    public required DateOnly TransactionDate { get; init; }

    public required decimal Value { get; init; }
    public required string Unit { get; init; }

    public required string MeasurementQuality { get; init; }

    public string? GoodsName { get; init; }
    public string? Comment { get; init; }
    public string? Specification { get; init; }
    public string? AccountNo { get; init; }
}