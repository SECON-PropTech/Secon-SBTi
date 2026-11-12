using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.DTOs.Scope312;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace api_tests;

public class ProductPeriodEntryTests
{
    [Fact]
    public async Task GetProductPeriodEntries_returns_only_requested_product_and_year()
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

        var service = new ProductMaterialQueryService(dbContext);
        var controller = new ProductMaterialsController(service);

        var response = await controller.GetProductPeriodEntries(
            product.Id,
            2025,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var entries = Assert.IsAssignableFrom<
            IReadOnlyList<ProductPeriodEntryResponse>>(okResult.Value);

        var entry = Assert.Single(entries);

        Assert.Equal(expectedEntry.Id, entry.Id);
        Assert.Equal(product.Id, entry.ProductId);
        Assert.Equal(25m, entry.Quantity);
        Assert.Equal(new DateOnly(2025, 1, 1), entry.Date);
        Assert.Equal("estimated", entry.MeasurementQuality);
        Assert.Equal("Manuellt uppskattat antal", entry.Comment);
    }
}