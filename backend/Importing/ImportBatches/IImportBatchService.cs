namespace SeconSbtiBackend.Importing.ImportBatches;

public interface IImportBatchService
{
    /// <summary>
    /// All previous imports from every source, newest first.
    /// </summary>
    Task<List<ImportBatchResponse>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reverts an import by deleting it together with every row it imported.
    /// NotFound if there is no import with the id, e.g. because it has already been reverted.
    /// LaterImportExists if it is a Mobigo import and there is a later Mobigo import.
    /// </summary>
    Task<ImportBatchDeleteResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
