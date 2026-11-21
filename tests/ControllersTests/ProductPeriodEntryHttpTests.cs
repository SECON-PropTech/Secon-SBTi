using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Models;

namespace api_tests;


/// <summary>
/// Testar PUT-endpointens felfall via HTTP mot PostgreSQL.
/// Fel produkt-ID ska ge 404.
/// Saknat antal, datum eller mätkvalitet ska ge 400 med fältspecifikt fel.
/// Registreringen ska förbli oförändrad i samtliga fall.
/// </summary>
public class ProductPeriodEntryHttpTests
{
    private const string ConnectionVariable = "SECON_TEST_DB_CONNECTION";

    public sealed class PostgreSqlFactAttribute : FactAttribute
    {
        public PostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(ConnectionVariable)))
            {
                Skip = $"Ange {ConnectionVariable} för att köra PostgreSQL-testet.";
            }
        }
    }

    public sealed class PostgreSqlTheoryAttribute : TheoryAttribute
    {
        public PostgreSqlTheoryAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(ConnectionVariable)))
            {
                Skip = $"Ange {ConnectionVariable} för att köra PostgreSQL-testet.";
            }
        }
    }

    [PostgreSqlFact]
    public async Task Update_returns_404_for_other_product_and_preserves_entry()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionVariable)!;

        var (entry, otherProductId) =
            await AddTestDataAsync(connectionString);

        try
        {
            using var factory = new HttpTestFactory(connectionString);
            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            using var response = await client.PutAsJsonAsync(
                $"/api/product-materials/{otherProductId}/entries/{entry.Id}",
                new
                {
                    quantity = 99m,
                    date = "2025-04-01",
                    measurementQuality = "measured",
                    comment = "Ska inte sparas"
                });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            await AssertEntryUnchangedAsync(connectionString, entry);
        }
        finally
        {
            await DeleteTestDataAsync(
                connectionString,
                entry,
                otherProductId);
        }
    }

    [PostgreSqlTheory]
    [InlineData("quantity")]
    [InlineData("date")]
    [InlineData("measurementQuality")]
    public async Task Update_returns_400_for_missing_required_field(
        string missingField)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionVariable)!;

        var (entry, otherProductId) =
            await AddTestDataAsync(connectionString);

        try
        {
            using var factory = new HttpTestFactory(connectionString);
            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            var body = new Dictionary<string, object?>
            {
                ["quantity"] = 99m,
                ["date"] = "2025-04-01",
                ["measurementQuality"] = "measured",
                ["comment"] = "Ska inte sparas"
            };

            body.Remove(missingField);

            using var response = await client.PutAsJsonAsync(
                $"/api/product-materials/{entry.ProductId}/entries/{entry.Id}",
                body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content
                .ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.NotNull(problem);
            Assert.Contains(missingField, problem.Errors.Keys);

            await AssertEntryUnchangedAsync(connectionString, entry);
        }
        finally
        {
            await DeleteTestDataAsync(
                connectionString,
                entry,
                otherProductId);
        }
    }

    private static ApplicationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(
        MobigoMaterialLine Entry,
        Guid OtherProductId)> AddTestDataAsync(string connectionString)
    {
        await using var dbContext = CreateContext(connectionString);

        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "HTTP-testprodukt",
            MeasurementQuality = "estimated"
        };

        var otherProduct = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "Annan HTTP-testprodukt",
            MeasurementQuality = "estimated"
        };

        var entry = new MobigoMaterialLine
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 12m,
            Date = new DateOnly(2025, 3, 1),
            MeasurementQuality = "estimated",
            Comment = "Ursprunglig kommentar"
        };

        dbContext.AddRange(product, otherProduct, entry);
        await dbContext.SaveChangesAsync();

        return (entry, otherProduct.Id);
    }

    private static async Task AssertEntryUnchangedAsync(
        string connectionString,
        MobigoMaterialLine expected)
    {
        await using var dbContext = CreateContext(connectionString);

        var actual = await dbContext.MobigoMaterialLines
            .AsNoTracking()
            .SingleAsync(row => row.Id == expected.Id);

        Assert.Equal(expected.ProductId, actual.ProductId);
        Assert.Equal(expected.Quantity, actual.Quantity);
        Assert.Equal(expected.Date, actual.Date);
        Assert.Equal(expected.MeasurementQuality, actual.MeasurementQuality);
        Assert.Equal(expected.Comment, actual.Comment);
        Assert.Equal(expected.ImportBatchId, actual.ImportBatchId);
        Assert.Equal(expected.MobigoWorkOrderId, actual.MobigoWorkOrderId);
    }

    private static async Task DeleteTestDataAsync(
        string connectionString,
        MobigoMaterialLine entry,
        Guid otherProductId)
    {
        await using var dbContext = CreateContext(connectionString);

        await dbContext.MobigoMaterialLines
            .Where(row => row.Id == entry.Id)
            .ExecuteDeleteAsync();

        await dbContext.SoldProducts
            .Where(product =>
                product.Id == entry.ProductId ||
                product.Id == otherProductId)
            .ExecuteDeleteAsync();
    }

    private sealed class HttpTestFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Anslutningen behövs även av applikationens uppstartskontroll.
            builder.ConfigureHostConfiguration(configuration =>
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            connectionString
                    }));

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<
                        AuthenticationSchemeOptions,
                        TestAuthenticationHandler>("Test", _ => { });
            });
        }
    }

    public sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(
            options,
            logger,
            encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        "scope312-http-test-user")
                ],
                Scheme.Name);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}