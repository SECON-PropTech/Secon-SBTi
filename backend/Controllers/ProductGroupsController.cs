using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;

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

    /// <summary>
    /// Creates a product group (FK-S.05).
    /// 201 with the new id when saved, 400 if the request is invalid.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(ProductGroupRequest request)
    {
        var productGroup = new ProductGroup
        {
            // Trimmed so "Fläktar " and "Fläktar" cannot become two groups.
            Name = request.Name!.Trim(),
            Description = request.Description
        };

        dbContext.ProductGroups.Add(productGroup);
        await dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new { productGroup.Id });
    }
}