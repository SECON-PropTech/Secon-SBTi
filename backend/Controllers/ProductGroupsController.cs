using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;

namespace SeconSbtiBackend.Controllers;

/// <summary>
/// Product groups that sold products are reported in (scope 3.11 and 3.12).
/// </summary>
[ApiController]
[Route("api/product-groups")]
public class ProductGroupsController(ApplicationDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// All product groups sorted by name.
    /// </summary>
    [HttpGet]
    public async Task<IReadOnlyList<ProductGroupResponse>> GetAll()
    {
        return await dbContext.ProductGroups
            .OrderBy(productGroup => productGroup.Name)
            .Select(productGroup => new ProductGroupResponse(productGroup.Id, productGroup.Name, productGroup.Description))
            .ToListAsync();
    }
}