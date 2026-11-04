using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/purchased-services")]
public class PurchasedServicesController(
    IPurchasedServicesService purchasedServicesService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    [ProducesResponseType(
        typeof(List<PurchasedServiceResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PurchasedServiceResponse>>>
        GetPurchasedServices(CancellationToken cancellationToken)
    {
        var services = await purchasedServicesService.GetAllAsync(
            cancellationToken);

        return Ok(services);
    }
}