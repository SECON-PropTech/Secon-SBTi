using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Importing.Visma4010;
using SeconSbtiBackend.Models;
using tests.ImportingTests.Visma4010;

namespace tests.ServicesTests.Visma4010ImportServiceTests;

/// <summary>
/// Shared helpers for the Visma4010ImportService tests.
/// </summary>
internal static class Visma4010ImportServiceTestHelpers
{
    public static readonly DateTime Jan1 = new(2026, 1, 1);

    /// <summary>
    /// Service with a context that has no database provider. Only for tests that fail before the database is used.
    /// </summary>
    public static Visma4010ImportService CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
        return new Visma4010ImportService(new ApplicationDbContext(options));
    }

    public static Visma4010ImportService CreateService(ApplicationDbContext context) => new(context);

    public static DbContextOptions<ApplicationDbContext> NewOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    public static AppUser CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        CreatedAt = DateTime.UtcNow
    };

    public static async Task<AppUser> SeedUserAsync(ApplicationDbContext context)
    {
        var user = CreateUser();
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// File builder that already contains account 4010.
    /// </summary>
    public static Visma4010FileBuilder Account() =>
        new Visma4010FileBuilder().Account("4010", "Inköp material");

    public static string HashOf(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}

/// <summary>
/// A stream whose reads fail, to check that I/O errors are not swallowed.
/// </summary>
internal sealed class ThrowingReadStream : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new IOException("Simulated stream failure.");
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
        => throw new IOException("Simulated stream failure.");
}

/// <summary>
/// A context whose SaveChangesAsync always fails. beforeFailure can change the database first,
/// to simulate another import saving between the check and the save.
/// </summary>
internal sealed class FailingSaveDbContext(
    DbContextOptions<ApplicationDbContext> options,
    Func<Task>? beforeFailure = null) : ApplicationDbContext(options)
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (beforeFailure is not null)
        {
            await beforeFailure();
        }

        throw new DbUpdateException("Simulated save failure.");
    }
}