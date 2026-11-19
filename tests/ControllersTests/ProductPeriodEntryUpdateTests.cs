using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;

namespace tests.ControllersTests;

public class ProductPeriodEntryUpdateTests
{
    private const string ConnectionVariable = "SECON_TEST_DB_CONNECTION";

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

    [PostgreSqlTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_saves_changes_and_preserves_origin(bool imported)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable(ConnectionVariable)!)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);

        // Alla testrader rullas tillbaka när transaktionen avslutas.
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync();

        var entry = await AddEntryAsync(dbContext, imported);
        var originalImportBatchId = entry.ImportBatchId;
        var originalWorkOrderId = entry.MobigoWorkOrderId;

        var controller = new MaterialLinesController(dbContext);

        var request = new MaterialLineRequest
        {
            ProductId = entry.ProductId,
            Quantity = 28m,
            Date = new DateOnly(2025, 4, 1),
            MeasurementQuality = "measured",
            Comment = "Korrigerat antal"
        };

        var result = await controller.Update(entry.Id, request);

        Assert.IsType<NoContentResult>(result);

        var savedEntry = await dbContext.MobigoMaterialLines
            .AsNoTracking()
            .SingleAsync(row => row.Id == entry.Id);

        Assert.Equal(request.ProductId.Value, savedEntry.ProductId);
        Assert.Equal(28m, savedEntry.Quantity);
        Assert.Equal(new DateOnly(2025, 4, 1), savedEntry.Date);
        Assert.Equal("measured", savedEntry.MeasurementQuality);
        Assert.Equal("Korrigerat antal", savedEntry.Comment);
        Assert.Equal(originalImportBatchId, savedEntry.ImportBatchId);
        Assert.Equal(originalWorkOrderId, savedEntry.MobigoWorkOrderId);

        var entries = await controller.GetByProductAndYear(
            entry.ProductId,
            2025,
            4);

        var response = Assert.Single(entries);
        Assert.Equal(entry.Id, response.Id);
        Assert.Equal(!imported, response.IsManual);
    }

    [PostgreSqlTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_removes_entry_and_returns_not_found_on_retry(
        bool imported)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable(ConnectionVariable)!)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);

        // Alla testrader rullas tillbaka när transaktionen avslutas.
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync();

        var entry = await AddEntryAsync(dbContext, imported);
        var controller = new MaterialLinesController(dbContext);

        var result = await controller.Delete(entry.Id);

        Assert.IsType<NoContentResult>(result);

        var exists = await dbContext.MobigoMaterialLines
            .AsNoTracking()
            .AnyAsync(row => row.Id == entry.Id);

        Assert.False(exists);

        if (entry.ImportBatchId is Guid importBatchId)
        {
            Assert.True(await dbContext.ImportBatches
                .AnyAsync(batch => batch.Id == importBatchId));
        }

        var repeatedResult = await controller.Delete(entry.Id);

        Assert.IsType<NotFoundResult>(repeatedResult);
    }

    private static async Task<MobigoMaterialLine> AddEntryAsync(
        ApplicationDbContext dbContext,
        bool imported)
    {
        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "Testprodukt",
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

        if (imported)
        {
            var importer = new AppUser
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Import",
                Email = $"material-line-{Guid.NewGuid():N}@example.invalid"
            };

            var importBatch = new ImportBatch
            {
                Id = Guid.NewGuid(),
                Source = "mobigo",
                ImportedBy = importer.Id
            };

            entry.ImportBatchId = importBatch.Id;
            entry.MobigoWorkOrderId = $"TEST-{Guid.NewGuid():N}";

            dbContext.AddRange(importer, importBatch);
        }

        dbContext.AddRange(product, entry);
        await dbContext.SaveChangesAsync();

        return entry;
    }
}