using Microsoft.EntityFrameworkCore;
using tests.DatabaseTests.PostgresTests.Infrastructure;

namespace tests.DatabaseTests.PostgresTests;

public sealed class QueryBehaviorTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Purchased_goods_can_be_filtered_and_ordered_by_transaction_date()
    {
        var supplier = await InsertSupplierAsync();
        var user = await InsertUserAsync();

        await ExecAsync("""
            INSERT INTO purchased_good(supplier_id,transaction_date,value,unit,measurement_quality,created_by)
            VALUES($1,DATE '2026-02-01',20,'sek','measured',$2),
                  ($1,DATE '2026-01-01',10,'sek','estimated',$2)
            """, supplier, user);

        using var db = CreateContext();
        var rows = await db.PurchasedGoods.AsNoTracking()
            .Where(x => x.SupplierId == supplier)
            .OrderBy(x => x.TransactionDate)
            .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), rows[0].TransactionDate);
        Assert.Equal(new DateOnly(2026, 2, 1), rows[1].TransactionDate);
    }

    [Fact]
    public async Task EF_context_reads_rows_written_directly_to_postgres()
    {
        var id = await InsertPurchasedGoodAsync();
        using var db = CreateContext();
        var row = await db.PurchasedGoods.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(100m, row.Value);
    }
}
