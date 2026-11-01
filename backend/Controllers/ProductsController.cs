using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;

namespace SeconSbtiBackend.Controllers;

/// <summary>
/// Sold products (sold_product). Shared by scope 3.11 and 3.12.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductsController(ApplicationDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Saves manually entered data for one product. All fields are replaced, so null clears a value.
    /// 204 when saved, 404 if the product does not exist, 400 if the group does not exist.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSoldProductRequest request)
    {
        var product = await dbContext.SoldProducts.FindAsync(id);

        if (product is null)
        {
            return NotFound();
        }

        if (request.GroupId is not null && !await dbContext.ProductGroups.AnyAsync(productGroup => productGroup.Id == request.GroupId))
        {
            ModelState.AddModelError(nameof(request.GroupId), $"Product group {request.GroupId} does not exist.");
            return ValidationProblem();
        }

        product.GroupId = request.GroupId;
        product.KwhPerYear = request.KwhPerYear;
        product.LifetimeYears = request.LifetimeYears;
        product.MeasurementQuality = request.MeasurementQuality;
        product.Comment = request.Comment;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}