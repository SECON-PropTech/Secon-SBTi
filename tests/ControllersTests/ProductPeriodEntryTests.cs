using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Models;

namespace tests.ControllersTests;

public class ProductPeriodEntryTests
{
    [Fact]
    public async Task GetByProductAndYear_returns_only_requested_product_and_year()
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

        var expectedEntry = new MobigoMaterialLine
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 25,
            Date = new DateOnly(2025, 1, 1),
            MeasurementQuality = "estimated",
            Comment = "Manuellt uppskattat antal"
        };

        dbContext.MobigoMaterialLines.AddRange(
            expectedEntry,
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = 10,
                Date = new DateOnly(2024, 12, 31),
                MeasurementQuality = "measured"
            },
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = 30,
                Date = new DateOnly(2026, 1, 1),
                MeasurementQuality = "measured"
            },
            new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ProductId = otherProduct.Id,
                Quantity = 50,
                Date = new DateOnly(2025, 6, 1),
                MeasurementQuality = "measured"
            }
        );

        await dbContext.SaveChangesAsync();

        var controller = new MaterialLinesController(dbContext);

        var entries = await controller.GetByProductAndYear(
            product.Id,
            2025);

        var entry = Assert.Single(entries);

        Assert.Equal(expectedEntry.Id, entry.Id);
        Assert.Equal(product.Id, entry.ProductId);
        Assert.Equal(25m, entry.Quantity);
        Assert.Equal(new DateOnly(2025, 1, 1), entry.Date);
        Assert.Equal("estimated", entry.MeasurementQuality);
        Assert.Equal("Manuellt uppskattat antal", entry.Comment);
        Assert.True(entry.IsManual);
    }

    [Fact]
    public async Task GetByProductAndYear_returns_empty_list_for_unknown_product()
    {
        using var dbContext = TestDatabase.Create();
        var controller = new MaterialLinesController(dbContext);

        var entries = await controller.GetByProductAndYear(
            Guid.NewGuid(),
            2025);

        Assert.Empty(entries);
    }
}