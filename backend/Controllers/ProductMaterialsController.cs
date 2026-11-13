using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SeconSbtiBackend.DTOs.Scope312;
using SeconSbtiBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;

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

    // Hämtar en produkts registreringar för det valda året.
    // Om månad anges begränsas hämtningen till den månaden.
    [HttpGet("{productId:guid}/entries")]
    public async Task<ActionResult<IReadOnlyList<ProductPeriodEntryResponse>>> GetProductPeriodEntries(
        Guid productId,
        [FromQuery, BindRequired, Range(2000, 2100)] int year,
        CancellationToken cancellationToken,
        [FromQuery, Range(1, 12)] int? month = null)
    {
        var entries = await productMaterialQueryService
            .GetProductPeriodEntriesAsync(productId, year, cancellationToken, month: month);

        // Tjänsten returnerar null om produkten saknas.
        if (entries is null)
        {
            return NotFound();
        }

        // En befintlig produkt utan registreringar för perioden ger en tom lista.
        return Ok(entries);
    }



    // Uppdaterar en manuell registrering som tillhör den valda produkten.
    [Authorize]
    [HttpPut("{productId:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateProductPeriodEntry(
        Guid productId,
        Guid entryId,
        [FromBody] UpdateProductPeriodEntryRequest request,
        [FromServices] ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var date = request.Date!.Value;

        // Använd samma årsintervall som vid hämtning av registreringar.
        if (date.Year is < 2000 or > 2100)
        {
            ModelState.AddModelError("date", "Ange ett datum med ett år mellan 2000 och 2100.");
            return ValidationProblem(ModelState);
        }

        var updatedRows = await dbContext.MobigoMaterialLines
            .Where(entry =>
                entry.Id == entryId &&
                entry.ProductId == productId &&
                entry.ImportBatchId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entry => entry.Quantity, request.Quantity!.Value)
                .SetProperty(entry => entry.Date, date)
                .SetProperty(entry => entry.MeasurementQuality, request.MeasurementQuality!)
                .SetProperty(entry => entry.Comment, request.Comment),
                cancellationToken);

        if (updatedRows == 0)
        {
            return NotFound();
        }

        return NoContent();
    }
}