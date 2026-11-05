using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;

namespace api_tests;

/// <summary>
/// Test helper: gives each test its own empty in-memory database, so the tests don't need PostgreSQL.
/// </summary>
public static class TestDatabase
{
    /// <summary>
    /// Creates a new, empty database with a unique name.
    /// </summary>
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}