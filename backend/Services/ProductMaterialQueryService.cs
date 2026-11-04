using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs.Scope312;

namespace SeconSbtiBackend.Services;

// Hämtar produktens materialuppgifter för Scope 3.12.
// Periodens antal och rapporteringsuppgifter hanteras inte här.
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

}