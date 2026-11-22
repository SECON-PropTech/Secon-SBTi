namespace SeconSbtiBackend.Importing.Visma4010;

public enum Visma4010ImportStatus
{
    Ok,
    Blocked,
    InvalidFile,
    InvalidUser
}

public sealed record Visma4010ImportResult(
    Visma4010ImportStatus Status,
    Visma4010ImportResponse? Response = null,
    string? Message = null);

public interface IVisma4010ImportService
{
    Task<Visma4010ImportResult> PreviewAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken);

    Task<Visma4010ImportResult> ImportAsync(
        Stream content,
        string fileName,
        Guid importedBy,
        CancellationToken cancellationToken);
}
