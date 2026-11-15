using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs.Scope312;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace api_tests;


/// <summary>
/// Testar redigering av periodregistreringar i Scope 3.12 mot PostgreSQL.
/// En manuell registrering ska ge 204 och spara ändringarna.
/// En importerad registrering ska ge 404 och behålla sina värden.
/// </summary>

public class ProductPeriodEntryUpdateTests

{
    private const string ConnectionVariable = "SECON_TEST_DB_CONNECTION";

    // xUnit markerar testet som överhoppat om testdatabasen saknar konfiguration.
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

    [PostgreSqlFact]
    public async Task UpdateProductPeriodEntry_keeps_imported_entry_unchanged()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionVariable)!;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);

        // Testets rader rullas tillbaka när transaktionen avslutas.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var importer = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Import",
            Email = $"scope312-{Guid.NewGuid():N}@example.invalid"
        };

        var importBatch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            Source = "mobigo",
            ImportedBy = importer.Id
        };

        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "Duschhandtag för test",
            MeasurementQuality = "estimated"
        };

        var entry = new MobigoMaterialLine
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ImportBatchId = importBatch.Id,
            MobigoWorkOrderId = $"TEST-{Guid.NewGuid():N}",
            Quantity = 12m,
            Date = new DateOnly(2025, 3, 1),
            MeasurementQuality = "measured",
            Comment = "Importerad registrering"
        };

        dbContext.AddRange(importer, importBatch, product, entry);
        await dbContext.SaveChangesAsync();

        var controller = new ProductMaterialsController(
            new ProductMaterialQueryService(dbContext));

        var request = new UpdateProductPeriodEntryRequest
        {
            Quantity = 28m,
            Date = new DateOnly(2025, 4, 1),
            MeasurementQuality = "estimated",
            Comment = "Försök att redigera"
        };

        // Använd rätt produkt och registrering för att testa just importskyddet.
        var result = await controller.UpdateProductPeriodEntry(
            product.Id,
            entry.Id,
            request,
            dbContext,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);

        // Läs om raden från databasen och kontrollera att värdena bevarades.
        var savedEntry = await dbContext.MobigoMaterialLines
            .AsNoTracking()
            .SingleAsync(row => row.Id == entry.Id);

        Assert.Equal(12m, savedEntry.Quantity);
        Assert.Equal(new DateOnly(2025, 3, 1), savedEntry.Date);
        Assert.Equal("measured", savedEntry.MeasurementQuality);
        Assert.Equal("Importerad registrering", savedEntry.Comment);
    }




    [PostgreSqlFact]
    public async Task UpdateProductPeriodEntry_saves_changes_to_manual_entry()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionVariable)!;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);

        // Testraderna rullas tillbaka när transaktionen avslutas.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "Duschhandtag för test",
            MeasurementQuality = "estimated"
        };

        var entry = new MobigoMaterialLine
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ImportBatchId = null,
            Quantity = 25m,
            Date = new DateOnly(2025, 3, 1),
            MeasurementQuality = "estimated",
            Comment = "Ursprunglig kommentar"
        };

        dbContext.AddRange(product, entry);
        await dbContext.SaveChangesAsync();

        var controller = new ProductMaterialsController(
            new ProductMaterialQueryService(dbContext));

        var request = new UpdateProductPeriodEntryRequest
        {
            Quantity = 28m,
            Date = new DateOnly(2025, 4, 1),
            MeasurementQuality = "measured",
            Comment = "Korrigerat antal"
        };

        var result = await controller.UpdateProductPeriodEntry(
            product.Id,
            entry.Id,
            request,
            dbContext,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        // Läs tillbaka raden från PostgreSQL för att kontrollera att ändringen sparades.
        var savedEntry = await dbContext.MobigoMaterialLines
            .AsNoTracking()
            .SingleAsync(row => row.Id == entry.Id);

        Assert.Equal(28m, savedEntry.Quantity);
        Assert.Equal(new DateOnly(2025, 4, 1), savedEntry.Date);
        Assert.Equal("measured", savedEntry.MeasurementQuality);
        Assert.Equal("Korrigerat antal", savedEntry.Comment);
    }
}