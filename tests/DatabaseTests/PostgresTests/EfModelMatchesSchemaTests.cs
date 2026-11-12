using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using tests.DatabaseTests.PostgresTests.Infrastructure;

namespace tests.DatabaseTests.PostgresTests;

public sealed class EfModelMatchesSchemaTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Every_mapped_table_and_column_exists_in_postgres()
    {
        using var context = CreateContext();
        var missing = new List<string>();

        foreach (var entity in context.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null) continue;

            var schema = entity.GetSchema() ?? "public";
            var store = StoreObjectIdentifier.Table(table, schema);

            var tableExists = await ScalarAsync<bool>("""
                SELECT EXISTS(SELECT 1 FROM information_schema.tables
                WHERE table_schema=$1 AND table_name=$2)
                """, schema, table);

            if (!tableExists)
            {
                missing.Add($"Table {schema}.{table}");
                continue;
            }

            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName(store);
                if (column is null) continue;

                var columnExists = await ScalarAsync<bool>("""
                    SELECT EXISTS(SELECT 1 FROM information_schema.columns
                    WHERE table_schema=$1 AND table_name=$2 AND column_name=$3)
                    """, schema, table, column);

                if (!columnExists)
                    missing.Add($"Column {schema}.{table}.{column} (EF property {property.Name})");
            }
        }

        Assert.True(missing.Count == 0, "EF model/schema drift:\n" + string.Join("\n", missing));
    }
}
