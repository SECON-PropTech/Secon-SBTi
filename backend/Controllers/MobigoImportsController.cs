using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Importing.Mobigo;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.Controllers;

/// <summary>
/// Import of delivered products from a Mobigo export, for scope 3.11 and 3.12.
/// </summary>
[ApiController]
[Route("api/imports/mobigo")]
[HasPermission(Permission.ImportMobigo)]
public class MobigoImportsController(IMobigoImportService importService) : ControllerBase
{
    // A little more than the largest allowed file, to leave room for the multipart form around it.
    private const long MaxRequestSize = MobigoImportService.MaxFileSize + 1024 * 1024;

    /// <summary>
    /// Reads the file and shows what an import would save, without saving anything.
    /// </summary>
    [HttpPost("preview")]
    [RequestSizeLimit(MaxRequestSize)]
    [ProducesResponseType(typeof(MobigoImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MobigoImportResponse>> Preview(
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

    /// <summary>
    /// Saves the lines of the file that are not already saved. 409 if the file has been imported before,
    /// 422 if something else stops the import. Both have the same body as the preview.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxRequestSize)]
    [ProducesResponseType(typeof(MobigoImportResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MobigoImportResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(MobigoImportResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MobigoImportResponse>> Import(
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

    private ActionResult<MobigoImportResponse> ToActionResult(
        MobigoImportResult result,
        int successStatusCode)
    {
        return result.Status switch
        {
            MobigoImportStatus.Ok =>
                StatusCode(successStatusCode, result.Response),
            MobigoImportStatus.Blocked when result.Response!.Blockers.Contains(MobigoBlockers.FileAlreadyImported) =>
                Conflict(result.Response),
            MobigoImportStatus.Blocked =>
                UnprocessableEntity(result.Response),
            MobigoImportStatus.InvalidFile =>
                this.FieldValidationProblem("file", result.Message!),
            MobigoImportStatus.InvalidUser =>
                Unauthorized(new { message = result.Message }),
            _ => throw new InvalidOperationException("Okänt resultat för import från Mobigo.")
        };
    }
}
