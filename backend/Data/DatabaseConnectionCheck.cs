using System.Net.Sockets;
using Npgsql;

namespace SeconSbtiBackend.Data;

/// <summary>
/// Startup check that the database can be reached, so a stopped PostgreSQL or a wrong
/// connection string gives one clear message instead of errors on the first request.
/// </summary>
public static class DatabaseConnectionCheck
{
    private const string SettingsHint =
        "The connection string is read from \"ConnectionStrings:DefaultConnection\" in backend/appsettings.Development.json.";

    /// <summary>
    /// Opens and closes one connection. Returns null if it worked, otherwise an explanation of what is wrong.
    /// </summary>
    public static async Task<string?> GetConnectionErrorAsync(string? connectionString, string environmentName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"""
                Cannot start: no database connection string was found.
                  Copy backend/appsettings.Development.json.example to backend/appsettings.Development.json and fill in your database details.
                  Current environment is "{environmentName}". appsettings.Development.json is only read when the environment is "Development".
                """;
        }

        NpgsqlConnectionStringBuilder settings;

        try
        {
            settings = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException ex)
        {
            return $"""
                Cannot start: the database connection string is not valid.
                  Reason: {ex.Message}
                  {SettingsHint}
                """;
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            return null;
        }
        catch (Exception ex)
        {
            // Never include the password in the message
            return $"""
                Cannot start: the database could not be reached.
                  Tried:  database "{settings.Database}" on {settings.Host}:{settings.Port} as user "{settings.Username}"
                  Reason: {Explain(ex, settings)}
                  {SettingsHint}
                """;
        }
    }

    private static string Explain(Exception ex, NpgsqlConnectionStringBuilder settings)
    {
        if (ex is PostgresException postgresError)
        {
            return postgresError.SqlState switch
            {
                PostgresErrorCodes.InvalidPassword =>
                    $"Wrong username or password for user \"{settings.Username}\".",
                PostgresErrorCodes.InvalidCatalogName =>
                    $"The database \"{settings.Database}\" does not exist. Create it and run the SQL in infrastructure/migrations.",
                _ => postgresError.MessageText,
            };
        }

        var socketError = FindInnerException<SocketException>(ex)?.SocketErrorCode;

        if (socketError == SocketError.ConnectionRefused)
        {
            return $"Nothing is listening on {settings.Host}:{settings.Port}. Is PostgreSQL running, and on that port?";
        }

        if (socketError == SocketError.HostNotFound)
        {
            return $"The host \"{settings.Host}\" could not be found. Check \"Host\" in the connection string.";
        }

        if (FindInnerException<TimeoutException>(ex) is not null)
        {
            return $"No answer from {settings.Host}:{settings.Port} within {settings.Timeout} seconds.";
        }

        return ex.Message;
    }

    // Npgsql often wraps the real cause (e.g. a SocketException) in an NpgsqlException
    private static T? FindInnerException<T>(Exception? ex) where T : Exception
    {
        while (ex is not null)
        {
            if (ex is T match)
            {
                return match;
            }

            ex = ex.InnerException;
        }

        return null;
    }
}
