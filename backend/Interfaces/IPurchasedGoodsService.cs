using SeconSbtiBackend.DTOs;

namespace SeconSbtiBackend.Interfaces;

public interface IPurchasedGoodsService
{
    Task<PurchasedGoodsResult<List<PurchasedGoodResponse>>> GetAllAsync(
        int? year,
        int? month,
        CancellationToken cancellationToken);

    Task<PurchasedGoodsResult<PurchasedGoodResponse>> CreateAsync(
        CreatePurchasedGoodRequest request,
        Guid createdBy,
        CancellationToken cancellationToken);

    Task<PurchasedGoodsResult<PurchasedGoodResponse>> UpdateAsync(
        Guid id,
        UpdatePurchasedGoodRequest request,
        CancellationToken cancellationToken);
}
