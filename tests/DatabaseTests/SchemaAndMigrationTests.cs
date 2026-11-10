using Npgsql;
using tests.DatabaseTests.Infrastructure;

namespace tests.DatabaseTests;

public sealed class SchemaStructureTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Theory]
    [InlineData("app_user")]
    [InlineData("user_role")]
    [InlineData("import_batch")]
    [InlineData("purchased_good")]
    [InlineData("facility_energy")]
    [InlineData("vehicle_usage")]
    [InlineData("waste")]
    [InlineData("business_travel_flight")]
    [InlineData("business_travel_ground")]
    [InlineData("attachment")]
    [InlineData("audit_log")]
    public async Task Required_table_exists(string table)
    {
        var exists = await ScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name=$1)",
            table);
        Assert.True(exists, $"Expected public.{table} to exist.");
    }

    [Fact]
    public async Task V2_columns_exist()
    {
        foreach (var (table, column) in new[]
        {
            ("sold_product", "measurement_quality"), ("sold_product", "comment"),
            ("mobigo_material_line", "measurement_quality"), ("mobigo_material_line", "comment"),
            ("purchased_service", "measurement_quality"), ("purchased_service", "comment")
        })
        {
            var exists = await ScalarAsync<bool>("""
                SELECT EXISTS(SELECT 1 FROM information_schema.columns
                WHERE table_schema='public' AND table_name=$1 AND column_name=$2)
                """, table, column);
            Assert.True(exists, $"Expected {table}.{column}.");
        }
    }
}

[Collection("db")]
public sealed class MigrationUpgradeTests(PostgresFixture fixture) : DbTestBase(fixture)
{

    [Fact]
    public async Task V1_to_V2_upgrade_adds_manual_entry_support()
    {
        var cs = await Fixture.CreateDatabaseAsync();
        await PostgresFixture.ApplyMigrationsAsync(cs, toVersion: 1);

        await using (var connection = new NpgsqlConnection(cs))
        {
            await connection.OpenAsync();
            await using var check = new NpgsqlCommand("""
                SELECT count(*) FROM information_schema.columns
                WHERE table_schema='public' AND table_name='sold_product'
                  AND column_name='measurement_quality'
                """, connection);
            Assert.Equal(0L, (long)(await check.ExecuteScalarAsync())!);
        }

        await PostgresFixture.ApplyMigrationsAsync(cs, fromVersion: 2);

        await using (var connection = new NpgsqlConnection(cs))
        {
            await connection.OpenAsync();
            await using var check = new NpgsqlCommand("""
                SELECT is_nullable FROM information_schema.columns
                WHERE table_schema='public' AND table_name='service_spend_report'
                  AND column_name='import_batch_id'
                """, connection);
            Assert.Equal("YES", await check.ExecuteScalarAsync());
        }
    }
}
