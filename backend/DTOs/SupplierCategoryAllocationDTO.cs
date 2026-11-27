namespace SeconSbtiBackend.DTOs;

public sealed record SupplierResponse(Guid Id, string Name);

public sealed record PurchaseCategoryResponse(Guid Id, string Name);

public sealed record SupplierCategoryAllocationResponse(
    Guid PurchaseCategoryId, string Name, decimal Share);

public sealed class UpdateSupplierCategoryAllocationsRequest
{
    public List<SupplierCategoryAllocationRequest>? Allocations { get; init; }
}

public sealed class SupplierCategoryAllocationRequest
{
    public Guid PurchaseCategoryId { get; init; }
    public decimal? Share { get; init; }
}

public enum SupplierCategoryAllocationStatus
{
    Success,
    ValidationError,
    NotFound
}

public sealed record SupplierCategoryAllocationResult(
    SupplierCategoryAllocationStatus Status,
    List<SupplierCategoryAllocationResponse>? Value = null,
    Dictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static SupplierCategoryAllocationResult Success(
        List<SupplierCategoryAllocationResponse> value) =>
        new(SupplierCategoryAllocationStatus.Success, Value: value);

    public static SupplierCategoryAllocationResult ValidationError(
        string field, string message) =>
        new(
            SupplierCategoryAllocationStatus.ValidationError,
            Errors: new Dictionary<string, string[]>
            {
                [field] = [message]
            });

    public static SupplierCategoryAllocationResult NotFound() =>
        new(
            SupplierCategoryAllocationStatus.NotFound,
            Message: "Leverantören finns inte.");
}
