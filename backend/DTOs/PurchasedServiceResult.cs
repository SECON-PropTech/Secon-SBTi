namespace SeconSbtiBackend.DTOs;

public enum PurchasedServiceStatus
{
    Success,
    ValidationError,
    NotFound
}

public sealed record PurchasedServiceResult(
    PurchasedServiceStatus Status,
    PurchasedServiceResponse? Value = null,
    Dictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static PurchasedServiceResult Success(PurchasedServiceResponse value) =>
        new(PurchasedServiceStatus.Success, Value: value);

    public static PurchasedServiceResult ValidationError(string field, string message) =>
        ValidationError(new Dictionary<string, string[]> { [field] = [message] });

    public static PurchasedServiceResult ValidationError(Dictionary<string, string[]> errors) =>
        new(PurchasedServiceStatus.ValidationError, Errors: errors);

    public static PurchasedServiceResult NotFound(string message) =>
        new(PurchasedServiceStatus.NotFound, Message: message);
}
