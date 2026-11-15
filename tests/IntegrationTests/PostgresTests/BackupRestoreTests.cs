
using Npgsql;
using tests.IntegrationTests.PostgresTests.Infrastructure;

namespace tests.PostgresTests;

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

        var dumpPath = Path.Combine(
            Path.GetTempPath(),
            $"postgres-tests-backup-{Guid.NewGuid():N}.dump");

        try
        {
            await CreateBackupAsync(dumpPath);
            var restoreCs = await Fixture.CreateDatabaseAsync();
            await RestoreBackupAsync(restoreCs, dumpPath);
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

     private async Task CreateBackupAsync(string dumpPath)
    {
        var builder = new NpgsqlConnectionStringBuilder(Fixture.ConnectionString);
        var dump = await Fixture.RunPostgresToolAsync(
            "pg_dump",
            Fixture.ConnectionString,
            "-h", GetRequiredValue(builder.Host, "Host"),
            "-p", builder.Port.ToString(),
            "-U", GetRequiredValue(builder.Username, "Username"),
            "-d", GetRequiredValue(builder.Database, "Database"),
            "-Fc",
            "-f", dumpPath);
    }

    private async Task RestoreBackupAsync(string restoreCs, string dumpPath)
    {
        var builder = new NpgsqlConnectionStringBuilder(restoreCs);
        var restore = await Fixture.RunPostgresToolAsync(
            "pg_restore",
            restoreCs,
            "-h", GetRequiredValue(builder.Host, "Host"),
            "-p", builder.Port.ToString(),
            "-U", GetRequiredValue(builder.Username, "Username"),
            "-d", GetRequiredValue(builder.Database, "Database"),
            "--no-owner",
            dumpPath);

        Assert.True(
            restore.ExitCode == 0,
            $"pg_restore failed: {restore.StandardError}");

    }
    private static string GetRequiredValue( string? value, string propertyName)
    {
        return value ?? throw new InvalidOperationException($"PostgreSQL {propertyName} is missing.");
    }
}
