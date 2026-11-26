namespace SeconSbtiBackend.Importing.ImportBatches;

/// <summary>
/// One previous import in the list of imports.
/// </summary>
/// <param name="Source">The import_batch source, e.g. "visma_4010". The frontend shows a label for it.</param>
/// <param name="RowCount">Rows the import has left today, the rows that a revert deletes.
/// Lower than at import time if rows have been deleted by hand since.</param>
/// <param name="CanRevert">False for a Mobigo import that has a later Mobigo import, which has to be reverted first.</param>
public record ImportBatchResponse(
    Guid Id,
    string Source,
    DateTime ImportedAt,
    string ImportedBy,
    List<string> FileNames,
    int RowCount,
    bool CanRevert);

public enum ImportBatchDeleteResult
{
    Deleted,
    NotFound,
    LaterImportExists
}
