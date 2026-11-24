using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SeconSbtiBackend.Models;
using tests.IntegrationTests.PostgresTests.Infrastructure;

namespace tests.ControllersTests;

/// <summary>
/// Testar validering och felfall för PUT /api/material-lines/{id}.
/// Saknade obligatoriska fält och okänd produkt ska ge 400.
/// Okänd registrering ska ge 404.
/// Befintliga registreringar ska förbli oförändrade vid fel.
/// </summary>
[Collection("db")]
public class MaterialLinesHttpTests(PostgresFixture fixture)
    : DbTestBase(fixture)
{
    [Theory]
    [InlineData("productId")]
    [InlineData("quantity")]
    [InlineData("date")]
    [InlineData("measurementQuality")]
    public async Task Update_returns_400_for_missing_required_field(
        string missingField)
    {
        var entry = await AddEntryAsync();

        try
        {
            using var factory =
                new MaterialLinesFactory(Fixture.ConnectionString);

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            var body = new Dictionary<string, object?>
            {
                ["productId"] = entry.ProductId,
                ["quantity"] = 28m,
                ["date"] = "2025-04-01",
                ["measurementQuality"] = "measured",
                ["comment"] = "Ska inte sparas"
            };

            body.Remove(missingField);

            using var response = await client.PutAsJsonAsync(
                $"/api/material-lines/{entry.Id}",
                body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content
                .ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.NotNull(problem);
            Assert.Contains(missingField, problem.Errors.Keys);

            await AssertEntryUnchangedAsync(entry);
        }
        finally
        {
            await DeleteTestDataAsync(entry);
        }
    }

    [Fact]
    public async Task Update_returns_400_for_unknown_product()
    {
        var entry = await AddEntryAsync();

        try
        {
            using var factory =
                new MaterialLinesFactory(Fixture.ConnectionString);

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            using var response = await client.PutAsJsonAsync(
                $"/api/material-lines/{entry.Id}",
                new
                {
                    productId = Guid.NewGuid(),
                    quantity = 28m,
                    date = "2025-04-01",
                    measurementQuality = "measured",
                    comment = "Ska inte sparas"
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content
                .ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.NotNull(problem);
            Assert.Contains("productId", problem.Errors.Keys);

            await AssertEntryUnchangedAsync(entry);
        }
        finally
        {
            await DeleteTestDataAsync(entry);
        }
    }

    [Fact]
    public async Task Update_returns_404_for_unknown_entry()
    {
        var entry = await AddEntryAsync();

        try
        {
            using var factory =
                new MaterialLinesFactory(Fixture.ConnectionString);

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            using var response = await client.PutAsJsonAsync(
                $"/api/material-lines/{Guid.NewGuid()}",
                new
                {
                    productId = entry.ProductId,
                    quantity = 28m,
                    date = "2025-04-01",
                    measurementQuality = "measured",
                    comment = "Ska inte sparas"
                });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            await AssertEntryUnchangedAsync(entry);
        }
        finally
        {
            await DeleteTestDataAsync(entry);
        }
    }

    private async Task<MobigoMaterialLine> AddEntryAsync()
    {
        await using var dbContext = CreateContext();

        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "HTTP-testprodukt",
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

        dbContext.AddRange(product, entry);
        await dbContext.SaveChangesAsync();

        return entry;
    }

    private async Task AssertEntryUnchangedAsync(MobigoMaterialLine expected)
    {
        await using var dbContext = CreateContext();

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

    private async Task DeleteTestDataAsync(MobigoMaterialLine entry)
    {
        await using var dbContext = CreateContext();

        await dbContext.MobigoMaterialLines
            .Where(row => row.Id == entry.Id)
            .ExecuteDeleteAsync();

        await dbContext.SoldProducts
            .Where(product => product.Id == entry.ProductId)
            .ExecuteDeleteAsync();
    }

    private sealed class MaterialLinesFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Uppstartskontrollen och API:t ska använda fixturens databas.
            builder.ConfigureHostConfiguration(configuration =>
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            connectionString
                    }));

            return base.CreateHost(builder);
        }
    }
}