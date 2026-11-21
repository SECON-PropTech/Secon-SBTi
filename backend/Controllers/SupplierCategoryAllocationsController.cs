using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;

namespace SeconSbtiBackend.Controllers;

[ApiController]
// TODO: Permissions
[Route("api/supplier-category-allocations")]
public class SupplierCategoryAllocationsController(
    ISupplierCategoryAllocationService service) : ControllerBase
{
    [HttpGet("suppliers")]
    public async Task<ActionResult<List<SupplierResponse>>> GetSuppliers(
        CancellationToken cancellationToken) =>
        Ok(await service.GetSuppliersAsync(cancellationToken));

    [HttpGet("categories")]
    public async Task<ActionResult<List<PurchaseCategoryResponse>>> GetCategories(
        CancellationToken cancellationToken) =>
        Ok(await service.GetCategoriesAsync(cancellationToken));

    [HttpGet("{supplierId:guid}")]
    [ProducesResponseType(
        typeof(List<SupplierCategoryAllocationResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SupplierCategoryAllocationResponse>>>
        GetAllocations(
            Guid supplierId,
            CancellationToken cancellationToken) =>
        ToActionResult(await service.GetAsync(
            supplierId, cancellationToken));

    [HttpPut("{supplierId:guid}")]
    [ProducesResponseType(
        typeof(List<SupplierCategoryAllocationResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SupplierCategoryAllocationResponse>>>
        UpdateAllocations(
            Guid supplierId,
            [FromBody] UpdateSupplierCategoryAllocationsRequest request,
            CancellationToken cancellationToken) =>
        ToActionResult(await service.UpdateAsync(
            supplierId, request, cancellationToken));

    private ActionResult<List<SupplierCategoryAllocationResponse>> ToActionResult(
        SupplierCategoryAllocationResult result) => result.Status switch
        {
            SupplierCategoryAllocationStatus.Success =>
                Ok(result.Value),

            SupplierCategoryAllocationStatus.ValidationError =>
                ValidationProblem(new ValidationProblemDetails(result.Errors!)
                {
                    Title = "En eller flera uppgifter behöver rättas.",
                    Status = StatusCodes.Status400BadRequest
                }),

            SupplierCategoryAllocationStatus.NotFound =>
                NotFound(new { message = result.Message }),

            _ => throw new InvalidOperationException(
                "Unknown supplier allocation result.")
        };
}