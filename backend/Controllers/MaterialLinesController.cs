using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Validation;

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
    /// The individual lines of one product in one calendar year, sorted by date (FK-DI.02).
    /// IsManual tells the frontend which lines can be changed or deleted.
    /// No product id, or a year outside 2000–2100, gives 400 Bad Request.
    /// </summary>
    [HttpGet]
    public async Task<IReadOnlyList<MaterialLineResponse>> GetByProductAndYear(
        [FromQuery, BindRequired] Guid productId,
        [FromQuery, BindRequired, Range(2000, 2100, ErrorMessage = "Ange ett år mellan 2000 och 2100.")] int year)
    {
        var yearStart = new DateOnly(year, 1, 1);
        var nextYearStart = new DateOnly(year + 1, 1, 1);

        return await dbContext.MobigoMaterialLines
            .Where(line => line.ProductId == productId && line.Date >= yearStart && line.Date < nextYearStart)
            .OrderBy(line => line.Date)
            .ThenBy(line => line.Id)
            .Select(line => new MaterialLineResponse(
                line.Id,
                line.ProductId,
                line.Quantity,
                line.Date,
                line.MeasurementQuality,
                line.Comment,
                // Null import batch means the line was registered manually.
                line.ImportBatchId == null))
            .ToListAsync();
    }


    /// <summary>
    /// Registers a quantity of a product manually (FK-DI.02).
    /// 201 with the new id when saved, 400 if the request is invalid or the product does not exist.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(MaterialLineRequest request)
    {
        // The product must exist, otherwise the foreign key would throw a 500 on save.
        // "productId" in camelCase so the error lands on the same field as the automatic validation errors.
        if (!await dbContext.SoldProducts.AnyAsync(product => product.Id == request.ProductId))
        {
            return this.FieldValidationProblem("productId", "Produkten finns inte.");
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

    /// <summary>
    /// Replaces all fields of a manually registered line. Imported lines cannot be changed here.
    /// 204 when saved, 404 if no manual line has that id, 400 if the request is invalid or the product does not exist.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, MaterialLineRequest request)
    {
        // Only manual lines (no import batch) may be edited, imported lines must match Mobigo.
        var materialLine = await dbContext.MobigoMaterialLines
            .FirstOrDefaultAsync(line => line.Id == id && line.ImportBatchId == null);

        if (materialLine is null)
        {
            return NotFound();
        }

        if (!await dbContext.SoldProducts.AnyAsync(product => product.Id == request.ProductId))
        {
            return this.FieldValidationProblem("productId", "Produkten finns inte.");
        }

        materialLine.ProductId = request.ProductId!.Value;
        materialLine.Quantity = request.Quantity!.Value;
        materialLine.Date = request.Date!.Value;
        materialLine.MeasurementQuality = request.MeasurementQuality!;
        materialLine.Comment = request.Comment;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Deletes a manually registered line. Imported lines cannot be deleted here.
    /// 204 when deleted, 404 if no manual line has that id.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Deletes straight in the database and returns how many rows were removed (0 or 1).
        var deletedCount = await dbContext.MobigoMaterialLines
            .Where(line => line.Id == id && line.ImportBatchId == null)
            .ExecuteDeleteAsync();

        return deletedCount == 0 ? NotFound() : NoContent();
    }
}