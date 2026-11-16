using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs.Scope312;

namespace SeconSbtiBackend.Services;


// Hämtar produktmaterial och produktens registreringar per år eller månad.

public sealed class ProductMaterialQueryService(ApplicationDbContext dbContext)
{
    // Returnerar null om produkten saknas.
    // CancellationToken gör att databasfrågorna kan avbrytas.
    public async Task<ProductMaterialsResponse?> GetProductMaterialsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        // Hämta bara produktuppgifterna som behövs i svaret.
        var product = await dbContext.SoldProducts
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ArticleNo
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return null;
        }

        // Hämta materialvikter och behandling via modellens relationer.
        // Sorteringen ger en stabil ordning även vid lika materialnamn.
        var materials = await dbContext.SoldProductMaterials
            .Where(m => m.ProductId == productId)
            .OrderBy(m => m.Material.Name)
            .ThenBy(m => m.MaterialId)
            .Select(m => new ProductMaterialResponse(
                m.MaterialId,
                m.Material.Name,
                m.WeightKg,
                m.Material.EolTreatmentId,
                m.Material.EolTreatment.Name
            ))
            .ToListAsync(cancellationToken);

        // En befintlig produkt utan material returneras med en tom lista.
        return new ProductMaterialsResponse(
            product.Id,
            product.Name,
            product.ArticleNo,
            materials
        );



    }

    // Hämtar alla produkter med deras material och slutbehandling.
    public async Task<IReadOnlyList<ProductMaterialsResponse>> GetAllProductMaterialsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await dbContext.SoldProducts
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Select(p => new ProductMaterialsResponse(
                p.Id,
                p.Name,
                p.ArticleNo,
                p.SoldProductMaterials
                    .OrderBy(m => m.Material.Name)
                    .ThenBy(m => m.MaterialId)
                    .Select(m => new ProductMaterialResponse(
                        m.MaterialId,
                        m.Material.Name,
                        m.WeightKg,
                        m.Material.EolTreatmentId,
                        m.Material.EolTreatment.Name
                    ))
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        return products;
    }




    // Hämtar produktens registreringar för ett år eller en viss månad.
    // Returnerar null om produkten saknas.
    public async Task<IReadOnlyList<ProductPeriodEntryResponse>?> GetProductPeriodEntriesAsync(
        Guid productId,
        int year,
        CancellationToken cancellationToken = default,
        int? month = null)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        var productExists = await dbContext.SoldProducts
            .AnyAsync(p => p.Id == productId, cancellationToken);

        if (!productExists)
        {
            return null;
        }

        var periodStart = new DateOnly(year, month ?? 1, 1);
        var periodEnd = month.HasValue
            ? periodStart.AddMonths(1)
            : periodStart.AddYears(1);

        var entries = await dbContext.MobigoMaterialLines
            .Where(entry =>
                entry.ProductId == productId &&
                entry.Date >= periodStart &&
                entry.Date < periodEnd)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Id)
            .Select(entry => new ProductPeriodEntryResponse(
                entry.Id,
                entry.ProductId,
                entry.Quantity,
                entry.Date,
                entry.MeasurementQuality,
                entry.Comment
            ))
            .ToListAsync(cancellationToken);

        return entries;
    }
}