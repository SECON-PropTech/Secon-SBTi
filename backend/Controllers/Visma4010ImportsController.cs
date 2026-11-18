using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Importing.Visma4010;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/imports/visma-4010")]
[HasPermission(Permission.ImportVisma4010)]
public class Visma4010ImportsController(IVisma4010ImportService importService) : ControllerBase
{
    // A little more than the largest allowed file, to leave room for the multipart form around it.
    private const long MaxRequestSize = Visma4010ImportService.MaxFileSize + 1024 * 1024;

    [HttpPost("preview")]
    [RequestSizeLimit(MaxRequestSize)]
    [ProducesResponseType(typeof(Visma4010ImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Visma4010ImportResponse>> Preview(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return this.FieldValidationProblem("file", "Välj en fil.");
        }

        await using var content = file.OpenReadStream();
        var result = await importService.PreviewAsync(content, file.FileName, cancellationToken);

        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpPost]
    [RequestSizeLimit(MaxRequestSize)]
    [ProducesResponseType(typeof(Visma4010ImportResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Visma4010ImportResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Visma4010ImportResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Visma4010ImportResponse>> Import(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var importedBy) || importedBy == Guid.Empty)
        {
            return Unauthorized(new
            {
                message = "Din inloggning kunde inte verifieras. Logga in igen."
            });
        }

        if (file is null)
        {
            return this.FieldValidationProblem("file", "Välj en fil.");
        }

        await using var content = file.OpenReadStream();
        var result = await importService.ImportAsync(content, file.FileName, importedBy, cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    private ActionResult<Visma4010ImportResponse> ToActionResult(
        Visma4010ImportResult result,
        int successStatusCode)
    {
        return result.Status switch
        {
            Visma4010ImportStatus.Ok =>
                StatusCode(successStatusCode, result.Response),
            Visma4010ImportStatus.Blocked when result.Response!.Blockers.Contains(Visma4010Blockers.FileAlreadyImported) =>
                Conflict(result.Response),
            Visma4010ImportStatus.Blocked =>
                UnprocessableEntity(result.Response),
            Visma4010ImportStatus.InvalidFile =>
                this.FieldValidationProblem("file", result.Message!),
            Visma4010ImportStatus.InvalidUser =>
                Unauthorized(new { message = result.Message }),
            _ => throw new InvalidOperationException("Okänt resultat för import från Visma.")
        };
    }
}
