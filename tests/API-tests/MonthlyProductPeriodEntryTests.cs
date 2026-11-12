using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.DTOs.Scope312;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace api_tests;

public class MonthlyProductPeriodEntryTests
{






    // Kontrollerar månadsfiltret för januari, skottårsfebruari och december.
    // Första och sista dagen i månaden ska komma med. Rader utanför
    // perioden eller för en annan produkt ska filtreras bort.
    // Kontrollerar också datumordningen och värdena i de returnerade raderna.
    [Theory]
    [InlineData(2025, 1)]
    [InlineData(2024, 2)]
    [InlineData(2025, 12)]
    public async Task GetProductPeriodEntries_returns_only_requested_month(
        int year,
        int month)
    {
        using var dbContext = TestDatabase.Create();

        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "Duschhandtag",
            MeasurementQuality = "estimated"
        };

        var otherProduct = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "UV-aggregat",
            MeasurementQuality = "estimated"
        };

        dbContext.SoldProducts.AddRange(product, otherProduct);

        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var firstEntry = new MobigoMaterialLine
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 0,
            Date = monthStart,
            MeasurementQuality = "measured",
            Comment = "Månadens första dag"
        };

        var lastEntry = new MobigoMaterialLine
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 25,
            Date = nextMonthStart.AddDays(-1),
            MeasurementQuality = "estimated",
            Comment = "Månadens sista dag"
        };

        dbContext.MobigoMaterialLines.AddRange(
            lastEntry,
            firstEntry,
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = 10,
                Date = monthStart.AddDays(-1),
                MeasurementQuality = "measured"
            },
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = 30,
                Date = nextMonthStart,
                MeasurementQuality = "measured"
            },
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = 40,
                Date = monthStart.AddYears(-1),
                MeasurementQuality = "measured"
            },
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = otherProduct.Id,
                Quantity = 50,
                Date = monthStart,
                MeasurementQuality = "measured"
            }
        );

        await dbContext.SaveChangesAsync();

        var service = new ProductMaterialQueryService(dbContext);
        var controller = new ProductMaterialsController(service);

        var response = await controller.GetProductPeriodEntries(
            product.Id,
            year,
            CancellationToken.None,
            month: month);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var entries = Assert.IsAssignableFrom<
            IReadOnlyList<ProductPeriodEntryResponse>>(okResult.Value);

        Assert.Collection(
            entries,
            entry =>
            {
                Assert.Equal(firstEntry.Id, entry.Id);
                Assert.Equal(product.Id, entry.ProductId);
                Assert.Equal(monthStart, entry.Date);
                Assert.Equal(0m, entry.Quantity);
                Assert.Equal("measured", entry.MeasurementQuality);
                Assert.Equal("Månadens första dag", entry.Comment);
            },
            entry =>
            {
                Assert.Equal(lastEntry.Id, entry.Id);
                Assert.Equal(product.Id, entry.ProductId);
                Assert.Equal(nextMonthStart.AddDays(-1), entry.Date);
                Assert.Equal(25m, entry.Quantity);
                Assert.Equal("estimated", entry.MeasurementQuality);
                Assert.Equal("Månadens sista dag", entry.Comment);
            });
    }



    // Kontrollerar att ett anrop utan månad returnerar båda raderna
    // från det valda året. En månad utan registreringar ska ge en tom lista.   s


    [Theory]
    [InlineData(null, 2)]
    [InlineData(2, 0)]
    public async Task GetProductPeriodEntries_handles_full_year_and_empty_month(
        int? month,
        int expectedCount)
    {
        using var dbContext = TestDatabase.Create();

        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "Duschhandtag",
            MeasurementQuality = "estimated"
        };

        dbContext.SoldProducts.Add(product);

        dbContext.MobigoMaterialLines.AddRange(
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = 10,
                Date = new DateOnly(2025, 1, 1),
                MeasurementQuality = "measured"
            },
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = 20,
                Date = new DateOnly(2025, 12, 31),
                MeasurementQuality = "estimated"
            }
        );

        await dbContext.SaveChangesAsync();

        var service = new ProductMaterialQueryService(dbContext);
        var controller = new ProductMaterialsController(service);

        var response = await controller.GetProductPeriodEntries(
            product.Id,
            2025,
            CancellationToken.None,
            month: month);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var entries = Assert.IsAssignableFrom<
            IReadOnlyList<ProductPeriodEntryResponse>>(okResult.Value);

        Assert.Equal(expectedCount, entries.Count);
    }
}