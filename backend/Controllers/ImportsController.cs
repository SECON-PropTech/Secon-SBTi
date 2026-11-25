using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Importing.ImportBatches;

namespace SeconSbtiBackend.Controllers;

/// <summary>
/// Previous imports from all sources. Importing a file is done in the controller of its source,
/// e.g. Visma4010ImportsController.
/// </summary>
[ApiController]
[Route("api/imports")]
public class ImportsController(IImportBatchService importBatchService) : ControllerBase
{
    /// <summary>
    /// All previous imports, newest first.
    /// </summary>
    [HttpGet]
    [HasPermission(Permission.ViewImports)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<List<ImportBatchResponse>> GetAll(CancellationToken cancellationToken)
    {
        return await importBatchService.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Reverts an import by deleting it and every row it imported, also rows that have been edited since.
    /// The same file can then be imported again. 404 if the import does not exist or has already been reverted.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permission.ManageImports)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await importBatchService.DeleteAsync(id, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
