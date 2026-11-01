using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;

namespace api_tests;

public class ProductsControllerTests
{
    /// <summary>
    /// Tests the Update method of the ProductsController class to make sure all values are saved.
    /// </summary>
    [Fact]
    public async Task Update_saves_the_new_values()
    {
        using var dbContext = TestDatabase.Create();
        var product = AddEmptyProduct(dbContext);
        var fansGroup = new ProductGroup { Id = Guid.NewGuid(), Name = "Fläktar" };
        dbContext.ProductGroups.Add(fansGroup);
        await dbContext.SaveChangesAsync();

        var result = await new ProductsController(dbContext).Update(product.Id, new UpdateSoldProductRequest
        {
            groupId = fansGroup.Id,
            KwhPerYear = 50,
            LifetimeYears = 10,
            MeasurementQuality = "measured",
            Comment = "Uppmätt 2025"
        });

        result.Should().BeOfType<NoContentResult>();
        product.GroupId.Should().Be(fansGroup.Id);
        product.KwhPerYear.Should().Be(50);
        product.LifetimeYears.Should().Be(10);
        product.MeasurementQuality.Should().Be("measured");
        product.Comment.Should().Be("Uppmätt 2025");
    }

    /// <summary>
    /// Tests the Update method of the ProductsController class to make sure null clears a value instead of turning it into 0 (IF-SG.03).
    /// </summary>
    [Fact]
    public async Task Update_clears_values_that_are_sent_as_null()
    {
        using var dbContext = TestDatabase.Create();
        var product = AddEmptyProduct(dbContext);
        product.KwhPerYear = 50;
        product.LifetimeYears = 10;
        await dbContext.SaveChangesAsync();

        await new ProductsController(dbContext).Update(product.Id, new UpdateSoldProductRequest { MeasurementQuality = "estimated" });

        product.KwhPerYear.Should().BeNull();
        product.LifetimeYears.Should().BeNull();
    }

    /// <summary>
    /// Tests the Update method of the ProductsController class to make sure an unknown product gives 404 Not Found.
    /// </summary>
    [Fact]
    public async Task Update_returns_not_found_for_unknown_product()
    {
        using var dbContext = TestDatabase.Create();

        var result = await new ProductsController(dbContext).Update(Guid.NewGuid(), new UpdateSoldProductRequest { MeasurementQuality = "estimated" });

        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests the Update method of the ProductsController class to make sure an unknown group gives 400 Bad Request and nothing is changed.
    /// </summary>
    [Fact]
    public async Task Update_returns_bad_request_for_unknown_group()
    {
        using var dbContext = TestDatabase.Create();
        var product = AddEmptyProduct(dbContext);
        await dbContext.SaveChangesAsync();

        var result = await new ProductsController(dbContext).Update(product.Id, new UpdateSoldProductRequest
        {
            groupId = Guid.NewGuid(),
            MeasurementQuality = "estimated"
        });

        var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.Value.Should().BeOfType<ValidationProblemDetails>().Which.Errors.Should().ContainKey(nameof(UpdateSoldProductRequest.groupId));
        product.GroupId.Should().BeNull();
    }

    /// <summary>
    /// Test helper: adds a product without any data, like one that just came in from Mobigo.
    /// </summary>
    private static SoldProduct AddEmptyProduct(ApplicationDbContext dbContext)
    {
        var product = new SoldProduct { Id = Guid.NewGuid(), Name = "Fläkt A", MeasurementQuality = "estimated" };
        dbContext.SoldProducts.Add(product);
        return product;
    }
}