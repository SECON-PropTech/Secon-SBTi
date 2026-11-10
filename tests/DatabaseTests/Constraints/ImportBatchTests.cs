using tests.DatabaseTests.Infrastructure;

namespace tests.DatabaseTests.Constraints;

public sealed class ImportBatchTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    public async Task ImportBatch_rejects_unknown_source(string source)
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation,
            "INSERT INTO import_batch(source,imported_by) VALUES($1,$2)", source, user);
    }

    [Fact]
    public async Task ImportBatch_requires_importer_and_valid_file_parent()
    {
        await AssertRejectedAsync(ForeignKeyViolation,
            "INSERT INTO import_batch(source,imported_by) VALUES('mobigo',$1)", Guid.NewGuid());

        await AssertRejectedAsync(ForeignKeyViolation,
            "INSERT INTO import_batch_file(import_batch_id,name,file_hash) VALUES($1,'x.csv','hash')", Guid.NewGuid());
    }

    [Fact]
    public async Task ImportBatchFile_file_hash_is_unique()
    {
        var batch = await InsertImportBatchAsync();
        await ExecAsync("INSERT INTO import_batch_file(import_batch_id,name,file_hash) VALUES($1,'a.csv','same-hash')", batch);
        await AssertRejectedAsync(UniqueViolation,
            "INSERT INTO import_batch_file(import_batch_id,name,file_hash) VALUES($1,'b.csv','same-hash')", batch);
    }
}
