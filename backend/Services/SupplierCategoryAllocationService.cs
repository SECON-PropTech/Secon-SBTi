using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Services;

public class SupplierCategoryAllocationService(ApplicationDbContext context)
    : ISupplierCategoryAllocationService
{
    public Task<List<SupplierResponse>> GetSuppliersAsync(
      Guid? purchaseCategoryId,
      CancellationToken cancellationToken)
    {
        var query = context.Suppliers.AsNoTracking();

        if (purchaseCategoryId is Guid categoryId)
        {
            query = query.Where(supplier =>
                supplier.SupplierCategoryAllocations.Any(allocation =>
                    allocation.PurchaseCategoryId == categoryId));
        }

        return query
            .OrderBy(supplier => supplier.Name)
            .ThenBy(supplier => supplier.Id)
            .Select(supplier => new SupplierResponse(
                supplier.Id, supplier.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<List<PurchaseCategoryResponse>> GetCategoriesAsync(
        CancellationToken cancellationToken) =>
        context.PurchaseCategories.AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new PurchaseCategoryResponse(
                category.Id, category.Name))
            .ToListAsync(cancellationToken);

    public async Task<SupplierCategoryAllocationResult> GetAsync(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var supplierExists = await context.Suppliers.AnyAsync(
            supplier => supplier.Id == supplierId,
            cancellationToken);

        if (!supplierExists)
            return SupplierCategoryAllocationResult.NotFound();

        var allocations = await context.SupplierCategoryAllocations
            .AsNoTracking()
            .Where(allocation => allocation.SupplierId == supplierId)
            .OrderBy(allocation => allocation.PurchaseCategory.Name)
            .ThenBy(allocation => allocation.PurchaseCategoryId)
            .Select(allocation => new SupplierCategoryAllocationResponse(
                allocation.PurchaseCategoryId,
                allocation.PurchaseCategory.Name,
                allocation.Share))
            .ToListAsync(cancellationToken);

        return SupplierCategoryAllocationResult.Success(allocations);
    }

    public async Task<SupplierCategoryAllocationResult> UpdateAsync(
        Guid supplierId,
        UpdateSupplierCategoryAllocationsRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);

        if (validationError is not null)
            return validationError;

        var allocations = request.Allocations!;
        var categoryIds = allocations
            .Select(allocation => allocation.PurchaseCategoryId)
            .ToList();

        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        // Serialize replacements for the same supplier so concurrent saves cannot mix allocations.
        var suppliers = await context.Suppliers
            .FromSqlInterpolated(
                $"SELECT * FROM supplier WHERE id = {supplierId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        if (suppliers.Count == 0)
            return SupplierCategoryAllocationResult.NotFound();

        var knownCategoryIds = await context.PurchaseCategories
            .Where(category => categoryIds.Contains(category.Id))
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);

        if (knownCategoryIds.Count != categoryIds.Count)
        {
            return SupplierCategoryAllocationResult.ValidationError(
                "allocations",
                "En eller flera kategorier finns inte. Välj befintliga kategorier.");
        }

        var existing = await context.SupplierCategoryAllocations
            .Where(allocation => allocation.SupplierId == supplierId)
            .ToDictionaryAsync(
                allocation => allocation.PurchaseCategoryId,
                cancellationToken);

        context.SupplierCategoryAllocations.RemoveRange(
            existing.Values.Where(allocation =>
                !categoryIds.Contains(allocation.PurchaseCategoryId)));

        foreach (var allocation in allocations)
        {
            if (existing.TryGetValue(
                allocation.PurchaseCategoryId, out var savedAllocation))
            {
                savedAllocation.Share = allocation.Share!.Value;
            }
            else
            {
                context.SupplierCategoryAllocations.Add(
                    new SupplierCategoryAllocation
                    {
                        SupplierId = supplierId,
                        PurchaseCategoryId = allocation.PurchaseCategoryId,
                        Share = allocation.Share!.Value
                    });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetAsync(supplierId, cancellationToken);
    }

    private static SupplierCategoryAllocationResult? Validate(
        UpdateSupplierCategoryAllocationsRequest request)
    {
        if (request.Allocations is not { Count: > 0 } allocations)
        {
            return SupplierCategoryAllocationResult.ValidationError(
                "allocations",
                "Ange minst en kategori och fördela totalt 100 procent.");
        }

        var categoryIds = new HashSet<Guid>();

        for (var index = 0; index < allocations.Count; index++)
        {
            var allocation = allocations[index];

            if (allocation is null)
            {
                return SupplierCategoryAllocationResult.ValidationError(
                    $"allocations[{index}]",
                    "Ange en kategori och en andel.");
            }

            if (allocation.PurchaseCategoryId == Guid.Empty)
            {
                return SupplierCategoryAllocationResult.ValidationError(
                    $"allocations[{index}].purchaseCategoryId",
                    "Välj en kategori.");
            }

            if (!categoryIds.Add(allocation.PurchaseCategoryId))
            {
                return SupplierCategoryAllocationResult.ValidationError(
                    $"allocations[{index}].purchaseCategoryId",
                    "Samma kategori får bara anges en gång.");
            }

            if (allocation.Share is not decimal share)
            {
                return SupplierCategoryAllocationResult.ValidationError(
                    $"allocations[{index}].share",
                    "Ange en andel.");
            }

            if (share <= 0m || share > 1m)
            {
                return SupplierCategoryAllocationResult.ValidationError(
                    $"allocations[{index}].share",
                    "Ange en andel större än 0 och högst 1, till exempel 0,2 för 20 procent.");
            }
        }

        var totalShare = allocations.Sum(allocation => allocation.Share!.Value);

        if (Math.Round(totalShare, 6) != 1m)
        {
            return SupplierCategoryAllocationResult.ValidationError(
                "allocations",
                "Fördelningen måste summera till 100 procent innan den kan sparas.");
        }

        return null;
    }
}
