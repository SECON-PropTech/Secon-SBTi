using tests.DatabaseTests.Infrastructure;

namespace tests.DatabaseTests.Constraints;

public sealed class SupplierCategoryTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    public async Task Allocation_share_must_be_greater_than_zero_and_at_most_one(double share)
    {
        var supplier = await InsertSupplierAsync();
        var category = await InsertPurchaseCategoryAsync();
        await AssertRejectedAsync(CheckViolation,
            "INSERT INTO supplier_category_allocation(supplier_id,purchase_category_id,share) VALUES($1,$2,$3)",
            supplier, category, (decimal)share);
    }

    [Fact]
    public async Task Allocation_rejects_duplicate_pair_and_missing_foreign_keys()
    {
        var supplier = await InsertSupplierAsync();
        var category = await InsertPurchaseCategoryAsync();
        await ExecAsync("INSERT INTO supplier_category_allocation(supplier_id,purchase_category_id,share) VALUES($1,$2,1)", supplier, category);
        await AssertRejectedAsync(UniqueViolation,
            "INSERT INTO supplier_category_allocation(supplier_id,purchase_category_id,share) VALUES($1,$2,1)", supplier, category);
        await AssertRejectedAsync(ForeignKeyViolation,
            "INSERT INTO supplier_category_allocation(supplier_id,purchase_category_id,share) VALUES($1,$2,1)", Guid.NewGuid(), category);
    }
}
