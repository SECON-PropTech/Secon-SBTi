namespace SeconSbtiBackend.DTOs
{
    public class PurchasedGoodResponse
    {

        public Guid Id { get; init; }
        public Guid SupplierId { get; init; }
        public DateOnly TransactionDate { get; init; }

        public decimal Value { get; init; }
        public string Unit { get; init; } = string.Empty;

        public string MeasurementQuality { get; init; } = string.Empty;
        public string? Comment { get; init; }
        public string? GoodsName { get; init; }
        public string? Specification { get; init; }
        public string? AccountNo { get; init; }

        public Guid CreatedBy { get; init; }
        public DateTime CreatedAt { get; init; }
    }

}
