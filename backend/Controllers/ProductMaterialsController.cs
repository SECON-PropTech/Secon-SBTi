using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs.Scope312;
using SeconSbtiBackend.Services;

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
}