using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs.Scope312;
using SeconSbtiBackend.Services;

using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/product-materials")]
public sealed class ProductMaterialsController(
    ProductMaterialQueryService productMaterialQueryService) : ControllerBase
{
    // Hämtar materialuppgifter för en enskild produkt.
    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductMaterialsResponse>> GetProductMaterials(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await productMaterialQueryService
            .GetProductMaterialsAsync(productId, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    // Hämtar alla produkter med deras materialuppgifter.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductMaterialsResponse>>> GetAllProductMaterials(
        CancellationToken cancellationToken)
    {
        var products = await productMaterialQueryService
            .GetAllProductMaterialsAsync(cancellationToken);

        return Ok(products);
    }


    /// <summary>
    /// Lägger till eller uppdaterar materialets vikt för en produkt.
    /// Vikten anges i kg per enskild produkt.
    /// Ger 204 när vikten sparats, 404 om produkten saknas
    /// och 400 om materialet eller requesten är ogiltig.
    /// </summary>
    [HttpPut("{productId:guid}/{materialId:guid}")]
    public async Task<IActionResult> UpdateMaterialWeight(
        Guid productId,
        Guid materialId,
        UpdateProductMaterialWeightRequest request,
        [FromServices] ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var productExists = await dbContext.SoldProducts
            .AnyAsync(product => product.Id == productId, cancellationToken);

        if (!productExists)
        {
            return NotFound();
        }

        var materialExists = await dbContext.Materials
            .AnyAsync(material => material.Id == materialId, cancellationToken);

        if (!materialExists)
        {
            return this.FieldValidationProblem(
                "materialId",
                "Materialet finns inte.");
        }

        // Samma produkt/material kan sparas igen utan att skapa dubbletter.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
        INSERT INTO sold_product_material (product_id, material_id, weight_kg)
        VALUES ({productId}, {materialId}, {request.WeightKg!.Value})
        ON CONFLICT (product_id, material_id)
        DO UPDATE SET weight_kg = EXCLUDED.weight_kg
        """,
            cancellationToken);

        return NoContent();
    }


}