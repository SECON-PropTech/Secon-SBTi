using tests.PostgresTests.Infrastructure;

namespace tests.PostgresTests.Constraints;

public sealed class PurchasedGoodTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    public async Task Measurement_quality_must_be_supported(string quality)
    {
        var supplier = await InsertSupplierAsync();
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO purchased_good(supplier_id,transaction_date,value,unit,measurement_quality,created_by)
            VALUES($1,DATE '2026-01-01',10,'sek',$2,$3)
            """, supplier, quality, user);
    }

    [Fact]
    public async Task Unit_must_not_be_blank()
    {
        var supplier = await InsertSupplierAsync();
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO purchased_good(supplier_id,transaction_date,value,unit,measurement_quality,created_by)
            VALUES($1,DATE '2026-01-01',10,'   ','measured',$2)
            """, supplier, user);
    }

    [Fact]
    public async Task Source_record_id_is_unique_and_negative_values_are_allowed()
    {
        var supplier = await InsertSupplierAsync();
        var user = await InsertUserAsync();
        var batch = await InsertImportBatchAsync(user);
        const string sql = """
            INSERT INTO purchased_good(supplier_id,transaction_date,value,unit,measurement_quality,created_by,import_batch_id,source_record_id)
            VALUES($1,DATE '2026-01-01',-10,'sek','estimated',$2,$3,'invoice-1')
            """;
        await ExecAsync(sql, supplier, user, batch);
        await AssertRejectedAsync(UniqueViolation, sql, supplier, user, batch);
    }

    [Fact]
    public async Task Supplier_and_creator_must_exist()
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(ForeignKeyViolation, """
            INSERT INTO purchased_good(supplier_id,transaction_date,value,unit,measurement_quality,created_by)
            VALUES($1,DATE '2026-01-01',10,'sek','measured',$2)
            """, Guid.NewGuid(), user);
    }
}
