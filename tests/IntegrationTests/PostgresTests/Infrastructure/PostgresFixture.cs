using Testcontainers.PostgreSql;
using System.Text.RegularExpressions;
using Npgsql;
using Xunit;

namespace tests.IntegrationTests.PostgresTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private static readonly Regex MigrationName =
        new(@"^V(\d+)__.+\.sql$", RegexOptions.Compiled);

    private readonly List<string> _databases = [];
    private readonly SemaphoreSlim _databaseLock = new(1, 1);

    private string? _adminConnectionString;
    private string? _connectionString;
    private readonly PostgreSqlContainer _postgres =
    new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString =>
        _connectionString
        ?? throw new InvalidOperationException("Fixture not initialized.");

    private string AdminConnectionString =>
        _adminConnectionString
        ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        //Start PostgreSQL inside Docker and connect to the PostgreSQL server inside the container
        await _postgres.StartAsync();
        var admin = new NpgsqlConnectionStringBuilder(
            _postgres.GetConnectionString())
        {
            Database = "postgres",
            Pooling = false
        };

        _adminConnectionString = admin.ConnectionString;
        await using (var connection =
            new NpgsqlConnection(AdminConnectionString))
        {
            await connection.OpenAsync();
        }

        // Create the test database and apply migrations
        _connectionString = await CreateDatabaseAsync();
        await ApplyMigrationsAsync(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        try
        {
            foreach (var database in _databases)
            {
                await using var connection = new NpgsqlConnection(AdminConnectionString);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand(
                    $"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            _databaseLock.Dispose();
            await _postgres.DisposeAsync();
        }
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
    /// Runs a command inside the PostgreSQL container (e.g. pg_dump, pg_restore).
    /// </summary>
    public async Task<ToolResult> RunInContainerAsync(params string[] command)
    {
        var result = await _postgres.ExecAsync(command);
        return new ToolResult((int)(result.ExitCode ?? -1), result.Stdout, result.Stderr);
    }

    public sealed record ToolResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}

[CollectionDefinition("db")]
public class DbCollection : ICollectionFixture<PostgresFixture>;
