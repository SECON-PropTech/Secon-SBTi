using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Services;
using tests.IntegrationTests.PostgresTests.Infrastructure;

namespace tests.IntegrationTests.PostgresTests;

public sealed class SupplierCategoryAllocationServiceTests(PostgresFixture fixture)
    : DbTestBase(fixture)
{
    [Fact]
    public async Task Filtering_by_either_category_includes_a_supplier_in_both()
    {
        var firstCategory = await InsertPurchaseCategoryAsync();
        var secondCategory = await InsertPurchaseCategoryAsync();
        var sharedSupplier = await InsertSupplierAsync(Unique("A"));
        var firstOnly = await InsertSupplierAsync(Unique("B"));
        var secondOnly = await InsertSupplierAsync(Unique("C"));

        await InsertAllocationAsync(sharedSupplier, firstCategory, 0.6m);
        await InsertAllocationAsync(sharedSupplier, secondCategory, 0.4m);
        await InsertAllocationAsync(firstOnly, firstCategory, 1m);
        await InsertAllocationAsync(secondOnly, secondCategory, 1m);

        await using var context = CreateContext();
        var service = new SupplierCategoryAllocationService(context);

        var first = await service.GetSuppliersAsync(firstCategory, CancellationToken.None);
        var second = await service.GetSuppliersAsync(secondCategory, CancellationToken.None);

        Assert.Equal(new[] { sharedSupplier, firstOnly }, first.Select(supplier => supplier.Id));
        Assert.Equal(new[] { sharedSupplier, secondOnly }, second.Select(supplier => supplier.Id));
    }

    [Fact]
    public async Task Update_replaces_allocations_and_persists_the_new_shares()
    {
        var supplier = await InsertSupplierAsync();
        var retainedCategory = await InsertPurchaseCategoryAsync();
        var removedCategory = await InsertPurchaseCategoryAsync();
        var addedCategory = await InsertPurchaseCategoryAsync();
        await InsertAllocationAsync(supplier, retainedCategory, 0.6m);
        await InsertAllocationAsync(supplier, removedCategory, 0.4m);

        var request = new UpdateSupplierCategoryAllocationsRequest
        {
            Allocations =
            [
                new() { PurchaseCategoryId = retainedCategory, Share = 0.25m },
                new() { PurchaseCategoryId = addedCategory, Share = 0.75m }
            ]
        };

        await using var context = CreateContext();
        var service = new SupplierCategoryAllocationService(context);

        var result = await service.UpdateAsync(supplier, request, CancellationToken.None);

        Assert.Equal(SupplierCategoryAllocationStatus.Success, result.Status);
        var saved = await ReadAllocationsAsync(supplier);
        Assert.Equal(2, saved.Count);
        Assert.Equal(0.25m, saved[retainedCategory]);
        Assert.Equal(0.75m, saved[addedCategory]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalid_update_leaves_existing_allocations_unchanged(bool hasUnknownCategory)
    {
        var supplier = await InsertSupplierAsync();
        var firstCategory = await InsertPurchaseCategoryAsync();
        var secondCategory = await InsertPurchaseCategoryAsync();
        await InsertAllocationAsync(supplier, firstCategory, 0.6m);
        await InsertAllocationAsync(supplier, secondCategory, 0.4m);

        var request = new UpdateSupplierCategoryAllocationsRequest
        {
            Allocations =
            [
                new() { PurchaseCategoryId = firstCategory, Share = 0.5m },
                new()
                {
                    PurchaseCategoryId = hasUnknownCategory ? Guid.NewGuid() : secondCategory,
                    Share = hasUnknownCategory ? 0.5m : 0.4m
                }
            ]
        };

        await using var context = CreateContext();
        var service = new SupplierCategoryAllocationService(context);

        var result = await service.UpdateAsync(supplier, request, CancellationToken.None);

        Assert.Equal(SupplierCategoryAllocationStatus.ValidationError, result.Status);
        var saved = await ReadAllocationsAsync(supplier);
        Assert.Equal(2, saved.Count);
        Assert.Equal(0.6m, saved[firstCategory]);
        Assert.Equal(0.4m, saved[secondCategory]);
    }

    private Task InsertAllocationAsync(Guid supplierId, Guid categoryId, decimal share) =>
        ExecAsync(
            "INSERT INTO supplier_category_allocation(supplier_id,purchase_category_id,share) VALUES($1,$2,$3)",
            supplierId, categoryId, share);

    private async Task<Dictionary<Guid, decimal>> ReadAllocationsAsync(Guid supplierId)
    {
        await using var context = CreateContext();
        return await context.SupplierCategoryAllocations
            .AsNoTracking()
            .Where(allocation => allocation.SupplierId == supplierId)
            .ToDictionaryAsync(allocation => allocation.PurchaseCategoryId, allocation => allocation.Share);
    }
}
