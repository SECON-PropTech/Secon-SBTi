using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;

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
                    + (batch.ServiceSpendReport == null ? 0 : batch.ServiceSpendReport.PurchasedServices.Count)))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        // Every foreign key to import_batch is ON DELETE CASCADE, so the database deletes the file
        // and all rows of the import together with the batch, in one statement.
        // Suppliers created by an import are not linked to it and are kept.
        var deleted = await context.ImportBatches
            .Where(batch => batch.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
