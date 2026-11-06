using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Controllers;

/// <summary>
/// Delivered quantities of sold products (mobigo_material_line) for scope 3.11.
/// Lines created here are manual, so they have no import batch.
/// </summary>
[ApiController]
[Route("api/material-lines")]
public class MaterialLinesController(ApplicationDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Registers a quantity of a product manually (FK-DI.02).
    /// 201 with the new id when saved, 400 if the request is invalid or the product does not exist.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(MaterialLineRequest request)
    {
        // The product must exist, otherwise the foreign key would throw a 500 on save.
        if (!await dbContext.SoldProducts.AnyAsync(product => product.Id == request.ProductId))
        {
            ModelState.AddModelError(nameof(request.ProductId), "Produkten finns inte.");
            return ValidationProblem();
        }

        var materialLine = new MobigoMaterialLine
        {
            // Null import batch marks the line as manually registered.
            ImportBatchId = null,
            ProductId = request.ProductId!.Value,
            Quantity = request.Quantity!.Value,
            Date = request.Date!.Value,
            MeasurementQuality = request.MeasurementQuality!,
            Comment = request.Comment
        };

        dbContext.MobigoMaterialLines.Add(materialLine);
        await dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new { materialLine.Id });
    }
}