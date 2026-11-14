using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Validation;

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
    /// 201 with the new id when saved, 400 if the request is invalid or the name is already used.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(ProductGroupRequest request)
    {
        // Trimmed so "Fläktar " and "Fläktar" cannot become two groups.
        var name = request.Name!.Trim();

        if (await NameIsTaken(name))
        {
            return this.FieldValidationProblem("name", "Det finns redan en produktgrupp med det namnet.");
        }

        var productGroup = new ProductGroup
        {
            Name = name,
            Description = request.Description
        };

        dbContext.ProductGroups.Add(productGroup);
        await dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new { productGroup.Id });
    }

    /// <summary>
    /// Replaces the name and description of a product group. Null clears the description.
    /// 204 when saved, 404 if the group does not exist, 400 if the request is invalid or the name is already used.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductGroupRequest request)
    {
        var productGroup = await dbContext.ProductGroups.FindAsync(id);

        if (productGroup is null)
        {
            return NotFound();
        }

        var name = request.Name!.Trim();

        if (await NameIsTaken(name, id))
        {
            return this.FieldValidationProblem("name", "Det finns redan en produktgrupp med det namnet.");
        }

        productGroup.Name = name;
        productGroup.Description = request.Description;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    // The unique index is case-sensitive, so "fläktar" would otherwise slip past it as a second group.
    // The group being updated is ignored, otherwise it could not keep its own name.
    private Task<bool> NameIsTaken(string name, Guid? ignoredProductGroupId = null)
    {
        return dbContext.ProductGroups
            .AnyAsync(productGroup => productGroup.Id != ignoredProductGroupId
                && productGroup.Name.ToLower() == name.ToLower());
    }
}