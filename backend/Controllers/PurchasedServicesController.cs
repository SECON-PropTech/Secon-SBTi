using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;

namespace SeconSbtiBackend.Controllers;

[ApiController]
// TODO: Permissions
[Route("api/purchased-services")]
public class PurchasedServicesController(IPurchasedServicesService purchasedServicesService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(List<PurchasedServiceResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PurchasedServiceResponse>>>
        GetPurchasedServices([FromQuery] PurchasedServicesQuery filter, CancellationToken cancellationToken)
    {
        var services = await purchasedServicesService.GetAllAsync(filter, cancellationToken);

        return Ok(services);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PurchasedServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PurchasedServiceResponse>> CreatePurchasedService(
        [FromBody] CreatePurchasedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await purchasedServicesService.CreateAsync(request, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{reportId:guid}")]
    [ProducesResponseType(typeof(PurchasedServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchasedServiceResponse>> UpdatePurchasedService(
        Guid reportId,
        [FromQuery, Required(ErrorMessage = "Ange ett kontonummer.")] string accountNo,
        [FromBody] UpdatePurchasedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await purchasedServicesService.UpdateAsync(
            reportId, accountNo, request, cancellationToken);

        return ToActionResult(result);
    }

    private ActionResult<PurchasedServiceResponse> ToActionResult(
        PurchasedServiceResult result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        return result.Status switch
        {
            PurchasedServiceStatus.Success => StatusCode(successStatusCode, result.Value),
            PurchasedServiceStatus.ValidationError =>
                ValidationProblem(new ValidationProblemDetails(result.Errors!)
                {
                    Title = "En eller flera uppgifter behöver rättas.",
                    Status = StatusCodes.Status400BadRequest
                }),
            PurchasedServiceStatus.NotFound => NotFound(new { message = result.Message }),
            _ => throw new InvalidOperationException("Okänt resultat för köpta tjänster.")
        };
    }
}
