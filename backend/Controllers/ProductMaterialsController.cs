using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Services;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/product-materials")]
public sealed class ProductMaterialsController(
    ProductMaterialQueryService productMaterialQueryService) : ControllerBase
{
}