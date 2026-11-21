using SeconSbtiBackend.DTOs;

namespace SeconSbtiBackend.Interfaces;

public interface ISupplierCategoryAllocationService
{
    Task<List<SupplierResponse>> GetSuppliersAsync(
        CancellationToken cancellationToken);

    Task<List<PurchaseCategoryResponse>> GetCategoriesAsync(
        CancellationToken cancellationToken);

    Task<SupplierCategoryAllocationResult> GetAsync(
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierCategoryAllocationResult> UpdateAsync(
        Guid supplierId,
        UpdateSupplierCategoryAllocationsRequest request,
        CancellationToken cancellationToken);
}