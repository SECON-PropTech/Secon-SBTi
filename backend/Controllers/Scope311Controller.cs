using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Calculations;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Dtos;

namespace SeconSbtiBackend.Controllers;

/// <summary>
/// Scope 3.11, use of sold products (FK-S.05).
/// </summary>
[ApiController]
[Route("api/scope311")]
public class Scope311Controller(ApplicationDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Report for one calendar year (FK-RE.3), based on the products sold that year.
    /// A year outside 2000–2100, or no year at all, gives 400 Bad Request.
    /// </summary>
    [HttpGet]
    public async Task<Scope311Response> Get([FromQuery, BindRequired, Range(2000, 2100)] int year)
    {
        var yearStart = new DateOnly(year, 1, 1);
        var nextYearStart = new DateOnly(year + 1, 1, 1);

        var productsSoldThisYear = await dbContext.SoldProducts
            .Include(product => product.Group)
            .Where(product => product.MobigoMaterialLines
                .Any(materialLine => materialLine.Date >= yearStart && materialLine.Date < nextYearStart))
            .Select(product => new
            {
                Product = product,
                Quantity = product.MobigoMaterialLines
                    .Where(materialLine => materialLine.Date >= yearStart && materialLine.Date < nextYearStart)
                    .Sum(materialLine => materialLine.Quantity)
            })
            .ToListAsync();

        var soldProducts = productsSoldThisYear
            .Select(soldProduct => (soldProduct.Product, soldProduct.Quantity))
            .ToList();

        return Scope311Calculator.Build(year, soldProducts);
    }
}