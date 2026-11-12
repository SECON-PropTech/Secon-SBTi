
using Npgsql;
using tests.DatabaseTests.PostgresTests.Infrastructure;

namespace tests.DatabaseTests.PostgresTests;

public sealed class BackupRestoreTests(PostgresFixture fixture)
    : DbTestBase(fixture)
{
    [Fact]
    public async Task Custom_format_dump_can_be_restored_into_a_new_database()
    {
        var marker = Unique("backup-marker");

        await ExecAsync(
            "INSERT INTO app_user(first_name,last_name,email) VALUES('Backup','Test',$1)",
            $"{marker}@test.se");

        var builder =
            new NpgsqlConnectionStringBuilder(Fixture.ConnectionString);

        var dumpPath = Path.Combine(
            Path.GetTempPath(),
            $"postgres-tests-backup-{Guid.NewGuid():N}.dump");

        try
        {
            var dump = await Fixture.RunPostgresToolAsync(
                "pg_dump",
                Fixture.ConnectionString,
                "-h", builder.Host,
                "-p", builder.Port.ToString(),
                "-U", builder.Username,
                "-d", builder.Database,
                "-Fc",
                "-f", dumpPath);

            Assert.True(
                dump.ExitCode == 0,
                $"pg_dump failed: {dump.StandardError}");

            var restoreCs = await Fixture.CreateDatabaseAsync();

            var restoreBuilder =
                new NpgsqlConnectionStringBuilder(restoreCs);

            var restore = await Fixture.RunPostgresToolAsync(
                "pg_restore",
                restoreCs,
                "-h", restoreBuilder.Host,
                "-p", restoreBuilder.Port.ToString(),
                "-U", restoreBuilder.Username,
                "-d", restoreBuilder.Database,
                "--no-owner",
                dumpPath);

            Assert.True(
                restore.ExitCode == 0,
                $"pg_restore failed: {restore.StandardError}");

            var count = await ScalarOnAsync<long>(
                restoreCs,
                "SELECT count(*) FROM app_user WHERE email=$1",
                $"{marker}@test.se");

            Assert.Equal(1L, count);
        }
        finally
        {
            if (File.Exists(dumpPath))
            {
                File.Delete(dumpPath);
            }
        }
    }
}
