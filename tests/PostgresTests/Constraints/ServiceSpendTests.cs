using tests.PostgresTests.Infrastructure;

namespace tests.PostgresTests.Constraints;

public sealed class ServiceSpendTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Report_period_to_cannot_precede_period_from()
    {
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO service_spend_report(period_from,period_to)
            VALUES(DATE '2026-09-01',DATE '2026-01-01')
            """);
    }

    [Fact]
    public async Task V2_allows_manual_report_without_import_batch()
    {
        var id = await ScalarAsync<Guid>("""
            INSERT INTO service_spend_report(import_batch_id,period_from,period_to)
            VALUES(NULL,DATE '2026-01-01',DATE '2026-09-30') RETURNING id
            """);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Purchased_service_requires_existing_report_and_unique_account_per_report()
    {
        await AssertRejectedAsync(ForeignKeyViolation,
            "INSERT INTO purchased_service(report_id,account_no,amount_sek) VALUES($1,'5010',100)", Guid.NewGuid());

        var report = await ScalarAsync<Guid>("""
            INSERT INTO service_spend_report(period_from,period_to)
            VALUES(DATE '2026-01-01',DATE '2026-12-31') RETURNING id
            """);
        await ExecAsync("INSERT INTO purchased_service(report_id,account_no,amount_sek) VALUES($1,'5010',100)", report);
        await AssertRejectedAsync(UniqueViolation,
            "INSERT INTO purchased_service(report_id,account_no,amount_sek) VALUES($1,'5010',200)", report);
    }
}
