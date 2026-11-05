using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/purchased-goods")]
public class PurchasedGoodsController(
    IPurchasedGoodsService purchasedGoodsService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(List<PurchasedGoodResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PurchasedGoodResponse>>> GetPurchasedGoods(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var result = await purchasedGoodsService.GetAllAsync(
            year, month, cancellationToken);

        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(PurchasedGoodResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PurchasedGoodResponse>> CreatePurchasedGood(
        [FromBody] CreatePurchasedGoodRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var createdBy) || createdBy == Guid.Empty)
        {
            return Unauthorized(new
            {
                message = "Din inloggning kunde inte verifieras. Logga in igen."
            });
        }

        var result = await purchasedGoodsService.CreateAsync(
            request, createdBy, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PurchasedGoodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PurchasedGoodResponse>> UpdatePurchasedGood(
        Guid id,
        [FromBody] UpdatePurchasedGoodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await purchasedGoodsService.UpdateAsync(
            id, request, cancellationToken);

        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(
        PurchasedGoodsResult<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        return result.Status switch
        {
            PurchasedGoodsResultStatus.Success =>
                StatusCode(successStatusCode, result.Value),
            PurchasedGoodsResultStatus.ValidationError =>
                this.FieldValidationProblem(result.Field!, result.Message!),
            PurchasedGoodsResultStatus.NotFound =>
                NotFound(new { message = result.Message }),
            PurchasedGoodsResultStatus.InvalidUser =>
                Unauthorized(new { message = result.Message }),
            _ => throw new InvalidOperationException("Okänt resultat för köpta varor.")
        };
    }
}
