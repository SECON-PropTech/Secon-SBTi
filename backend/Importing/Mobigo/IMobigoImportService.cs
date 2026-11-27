namespace SeconSbtiBackend.Importing.Mobigo;

public enum MobigoImportStatus
{
    Ok,
    Blocked,
    InvalidFile,
    InvalidUser
}

public sealed record MobigoImportResult(
    MobigoImportStatus Status,
    MobigoImportResponse? Response = null,
    string? Message = null);

public interface IMobigoImportService
{
    Task<MobigoImportResult> PreviewAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken);

    Task<MobigoImportResult> ImportAsync(
        Stream content,
        string fileName,
        Guid importedBy,
        CancellationToken cancellationToken);
}
