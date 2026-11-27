using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Importing.Mobigo;

namespace SeconSbtiBackend.Importing.ImportBatches;

public sealed class ImportBatchService(ApplicationDbContext context) : IImportBatchService
{
    public async Task<List<ImportBatchResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.ImportBatches
            .AsNoTracking()
            .OrderByDescending(batch => batch.CreatedAt)
            .Select(batch => new ImportBatchResponse(
                batch.Id,
                batch.Source,
                batch.CreatedAt,
                batch.ImportedByNavigation.FirstName + " " + batch.ImportedByNavigation.LastName,
                batch.ImportBatchFiles
                    .OrderBy(file => file.Name)
                    .Select(file => file.Name)
                    .ToList(),
                // Every table that an import can save rows to. Only one of them has rows for a given import.
                batch.PurchasedGoods.Count
                    + batch.MobigoMaterialLines.Count
                    + batch.VehicleUsages.Count
                    + (batch.ServiceSpendReport == null ? 0 : batch.ServiceSpendReport.PurchasedServices.Count),
                batch.Source != MobigoImportService.Source
                    || !context.ImportBatches.Any(later =>
                        later.Source == MobigoImportService.Source && later.CreatedAt > batch.CreatedAt)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ImportBatchDeleteResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var batch = await context.ImportBatches
            .AsNoTracking()
            .Where(batch => batch.Id == id)
            .Select(batch => new { batch.Source, batch.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (batch is null)
        {
            return ImportBatchDeleteResult.NotFound;
        }

        // Mobigo exports overlap. A later import skips the lines an earlier import has already saved,
        // so those lines belong to the earlier import only. Reverting it would delete deliveries that the later
        // file also contains, and the later file can't be imported again. So only the latest Mobigo import can be reverted.
        if (batch.Source == MobigoImportService.Source
            && await context.ImportBatches.AnyAsync(
                later => later.Source == MobigoImportService.Source && later.CreatedAt > batch.CreatedAt,
                cancellationToken))
        {
            return ImportBatchDeleteResult.LaterImportExists;
        }

        // Every foreign key to import_batch is ON DELETE CASCADE, so the database deletes the file
        // and all rows of the import together with the batch, in one statement.
        // Suppliers created by an import are not linked to it and are kept.
        var deleted = await context.ImportBatches
            .Where(batch => batch.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0 ? ImportBatchDeleteResult.Deleted : ImportBatchDeleteResult.NotFound;
    }
}
