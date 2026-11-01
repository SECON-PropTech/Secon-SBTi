namespace SeconSbtiBackend.DTOs
{
    public class PurchasedGoodCategoryResponse
    {
        public Guid PurchaseCategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Share { get; init; }
    }
}