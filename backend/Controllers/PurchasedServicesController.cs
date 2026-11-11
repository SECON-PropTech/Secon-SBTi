using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/purchased-services")]
public class PurchasedServicesController(IPurchasedServicesService purchasedServicesService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(List<PurchasedServiceResponse>),
        StatusCodes.Status200OK)]
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
        var response = await purchasedServicesService.CreateAsync(request, cancellationToken);

        if (response is null)
        {
            return this.FieldValidationProblem(
                "accountNo",
                "Kontot saknar en kopplad tjänstekategori. Välj ett kategoriserat konto.");
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
