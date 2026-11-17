using Npgsql;
using tests.IntegrationTests.PostgresTests.Infrastructure;

namespace tests.IntegrationTests.PostgresTests;

public sealed class BackupRestoreTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Custom_format_dump_can_be_restored_into_a_new_database()
    {
        var marker = Unique("backup-marker");
        await ExecAsync(
            "INSERT INTO app_user(first_name,last_name,email) VALUES('Backup','Test',$1)",
            $"{marker}@test.se");

        var sourceDb = new NpgsqlConnectionStringBuilder(Fixture.ConnectionString).Database!;
        var dumpPath = $"/tmp/backup-{Guid.NewGuid():N}.dump"; // path inside the container

        // Both tools run inside the container, so the dump file never leaves it.
        var dump = await Fixture.RunInContainerAsync(
            "pg_dump", "-U", "postgres", "-d", sourceDb, "-Fc", "-f", dumpPath);
        Assert.True(dump.ExitCode == 0, $"pg_dump failed: {dump.StandardError}");

        var restoreCs = await Fixture.CreateDatabaseAsync();
        var restoreDb = new NpgsqlConnectionStringBuilder(restoreCs).Database!;
        var restore = await Fixture.RunInContainerAsync(
            "pg_restore", "-U", "postgres", "-d", restoreDb, "--no-owner", dumpPath);
        Assert.True(restore.ExitCode == 0, $"pg_restore failed: {restore.StandardError}");

        var count = await ScalarOnAsync<long>(
            restoreCs,
            "SELECT count(*) FROM app_user WHERE email=$1",
            $"{marker}@test.se");

        Assert.Equal(1L, count);
    }
}