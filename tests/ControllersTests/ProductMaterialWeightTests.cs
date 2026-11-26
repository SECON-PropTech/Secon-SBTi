using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs.Scope312;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;
using tests.IntegrationTests.PostgresTests.Infrastructure;

namespace tests.ControllersTests;

/// <summary>
/// Testar tillägg och uppdatering av materialvikt mot PostgreSQL.
/// Okänd produkt eller okänt material ska avvisas utan att skapa kopplingar.
/// </summary>
[Collection("db")]
public class ProductMaterialWeightTests(PostgresFixture fixture)
    : DbTestBase(fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateMaterialWeight_saves_weight_and_handles_repeated_calls(
        bool existingAllocation)
    {
        await using var dbContext = CreateContext();

        // Alla testrader rullas tillbaka när transaktionen avslutas.
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync();

        var (product, material) = await AddTestDataAsync(dbContext);

        if (existingAllocation)
        {
            dbContext.SoldProductMaterials.Add(new SoldProductMaterial
            {
                ProductId = product.Id,
                MaterialId = material.Id,
                WeightKg = 1m
            });

            await dbContext.SaveChangesAsync();
        }

        var controller = new ProductMaterialsController(
            new ProductMaterialQueryService(dbContext));

        var result = await controller.UpdateMaterialWeight(
            product.Id,
            material.Id,
            new UpdateProductMaterialWeightRequest
            {
                WeightKg = 0.45m
            },
            dbContext,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var saved = await dbContext.SoldProductMaterials
            .AsNoTracking()
            .SingleAsync(row =>
                row.ProductId == product.Id &&
                row.MaterialId == material.Id);

        Assert.Equal(0.45m, saved.WeightKg);

        var zeroWeightRequest = new UpdateProductMaterialWeightRequest
        {
            WeightKg = 0m
        };

        var updateResult = await controller.UpdateMaterialWeight(
            product.Id,
            material.Id,
            zeroWeightRequest,
            dbContext,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(updateResult);

        var repeatedResult = await controller.UpdateMaterialWeight(
            product.Id,
            material.Id,
            zeroWeightRequest,
            dbContext,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(repeatedResult);

        var savedAgain = await dbContext.SoldProductMaterials
            .AsNoTracking()
            .SingleAsync(row =>
                row.ProductId == product.Id &&
                row.MaterialId == material.Id);

        Assert.Equal(0m, savedAgain.WeightKg);
    }

    [Fact]
    public async Task UpdateMaterialWeight_returns_404_for_unknown_product()
    {
        await using var dbContext = CreateContext();

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync();

        var (product, material) = await AddTestDataAsync(dbContext);

        var controller = new ProductMaterialsController(
            new ProductMaterialQueryService(dbContext));

        var result = await controller.UpdateMaterialWeight(
            Guid.NewGuid(),
            material.Id,
            new UpdateProductMaterialWeightRequest
            {
                WeightKg = 0.45m
            },
            dbContext,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);

        Assert.False(await dbContext.SoldProductMaterials
            .AnyAsync(row => row.MaterialId == material.Id));

        Assert.True(await dbContext.SoldProducts
            .AnyAsync(row => row.Id == product.Id));
    }

    [Fact]
    public async Task UpdateMaterialWeight_returns_400_for_unknown_material()
    {
        await using var dbContext = CreateContext();

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync();

        var (product, _) = await AddTestDataAsync(dbContext);

        var controller = new ProductMaterialsController(
            new ProductMaterialQueryService(dbContext));

        var result = await controller.UpdateMaterialWeight(
            product.Id,
            Guid.NewGuid(),
            new UpdateProductMaterialWeightRequest
            {
                WeightKg = 0.45m
            },
            dbContext,
            CancellationToken.None);

        var error = Assert.IsAssignableFrom<ObjectResult>(result);

        Assert.Equal(400, error.StatusCode);

        var problem = Assert.IsType<ValidationProblemDetails>(error.Value);
        Assert.Contains("materialId", problem.Errors.Keys);

        Assert.False(await dbContext.SoldProductMaterials
            .AnyAsync(row => row.ProductId == product.Id));
    }

    private static async Task<(SoldProduct Product, Material Material)>
        AddTestDataAsync(ApplicationDbContext dbContext)
    {
        var treatment = new WasteTreatment
        {
            Id = Guid.NewGuid(),
            Name = $"Testbehandling-{Guid.NewGuid():N}"
        };

        var material = new Material
        {
            Id = Guid.NewGuid(),
            Name = $"Testmaterial-{Guid.NewGuid():N}",
            EolTreatmentId = treatment.Id
        };

        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "Testprodukt för materialvikt",
            MeasurementQuality = "estimated"
        };

        dbContext.AddRange(treatment, material, product);
        await dbContext.SaveChangesAsync();

        return (product, material);
    }
}