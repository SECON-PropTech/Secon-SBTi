using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs.Scope312;
using SeconSbtiBackend.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/product-materials")]
public sealed class ProductMaterialsController(
    ProductMaterialQueryService productMaterialQueryService) : ControllerBase
{
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



    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductMaterialsResponse>>> GetAllProductMaterials(
    CancellationToken cancellationToken)
    {
        var products = await productMaterialQueryService
            .GetAllProductMaterialsAsync(cancellationToken);

        return Ok(products);
    }


    [HttpGet("{productId:guid}/entries")]
    public async Task<ActionResult<IReadOnlyList<ProductPeriodEntryResponse>>> GetProductPeriodEntries(
    Guid productId,
    [FromQuery, BindRequired, Range(2000, 2100)] int year,
    CancellationToken cancellationToken)
    {
        var entries = await productMaterialQueryService
            .GetProductPeriodEntriesAsync(
                productId,
                year,
                cancellationToken);

        if (entries is null)
        {
            return NotFound();
        }

        return Ok(entries);
    }
}