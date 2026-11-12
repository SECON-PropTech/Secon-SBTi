
using System.Text.RegularExpressions;
using Npgsql;
using Xunit;

namespace tests.DatabaseTests.PostgresTests.Infrastructure;

/// <summary>
/// Uses a locally installed PostgreSQL server, creates isolated test databases,
/// and applies the SQL migrations.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private static readonly Regex MigrationName =
        new(@"^V(\d+)__.+\.sql$", RegexOptions.Compiled);

    private readonly List<string> _databases = [];
    private readonly SemaphoreSlim _databaseLock = new(1, 1);

    private string? _adminConnectionString;
    private string? _connectionString;

    private static string PostgresBin =>
        Environment.GetEnvironmentVariable("POSTGRES_BIN")
        ?? @"C:\Program Files\PostgreSQL\16\bin";

    public string ConnectionString =>
        _connectionString
        ?? throw new InvalidOperationException("Fixture not initialized.");

    private string AdminConnectionString =>
        _adminConnectionString
        ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        var configuredConnection =
            Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(configuredConnection))
        {
            throw new InvalidOperationException(
                "Set the POSTGRES_TEST_CONNECTION environment variable.");
        }

        var admin = new NpgsqlConnectionStringBuilder(configuredConnection)
        {
            Database = "postgres",
            Pooling = false
        };

        _adminConnectionString = admin.ConnectionString;

        // Verify PostgreSQL is reachable.
        await using (var connection =
            new NpgsqlConnection(AdminConnectionString))
        {
            await connection.OpenAsync();
        }

        _connectionString = await CreateDatabaseAsync();

        await ApplyMigrationsAsync(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        foreach (var database in _databases)
        {
            await using var connection =
                new NpgsqlConnection(AdminConnectionString);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)",
                connection);

            await command.ExecuteNonQueryAsync();
        }

        _databaseLock.Dispose();
    }

    /// <summary>
    /// Returns all migration files ordered by version number.
    /// </summary>
    public static IReadOnlyList<(int Version, string FilePath)> MigrationFiles()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "migrations");

        var files = Directory.GetFiles(dir, "*.sql")
            .Select(file => (
                Match: MigrationName.Match(Path.GetFileName(file)),
                FilePath: file))
            .Where(x => x.Match.Success)
            .Select(x => (
                Version: int.Parse(x.Match.Groups[1].Value),
                x.FilePath))
            .OrderBy(x => x.Version)
            .ToList();

        if (files.Count == 0)
        {
            throw new InvalidOperationException(
                $"No migration files found in {dir}. Check the <Content> item in the test .csproj.");
        }

        return files;
    }

    /// <summary>
    /// Applies migrations, optionally limited to a version range.
    /// </summary>
    public static async Task ApplyMigrationsAsync(
        string connectionString,
        int? fromVersion = null,
        int? toVersion = null)
    {
        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        foreach (var (version, filePath) in MigrationFiles())
        {
            if (version < (fromVersion ?? int.MinValue) ||
                version > (toVersion ?? int.MaxValue))
            {
                continue;
            }

            await using var command = new NpgsqlCommand(
                await File.ReadAllTextAsync(filePath),
                connection);

            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Creates an isolated database and returns its connection string.
    /// </summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"test_{Guid.NewGuid():N}";

        await using var connection =
            new NpgsqlConnection(AdminConnectionString);

        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            $"CREATE DATABASE \"{name}\"",
            connection);

        await command.ExecuteNonQueryAsync();

        await _databaseLock.WaitAsync();
        try
        {
            _databases.Add(name);
        }
        finally
        {
            _databaseLock.Release();
        }

        return new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Database = name,
            Pooling = false
        }.ConnectionString;
    }

    /// <summary>
    /// Runs a local PostgreSQL utility such as pg_dump or pg_restore.
    /// </summary>
    public async Task<ToolResult> RunPostgresToolAsync(
        string tool,
        string connectionString,
        params string[] arguments)
    {
        var builder =
            new NpgsqlConnectionStringBuilder(connectionString);

        var executable = Path.Combine(PostgresBin, $"{tool}.exe");

        if (!File.Exists(executable))
        {
            throw new FileNotFoundException(
                $"PostgreSQL utility not found: {executable}");
        }

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment["PGPASSWORD"] = builder.Password;

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new System.Diagnostics.Process
        {
            StartInfo = startInfo
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ToolResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }

    public sealed record ToolResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}

[CollectionDefinition("db")]
public class DbCollection : ICollectionFixture<PostgresFixture>;
