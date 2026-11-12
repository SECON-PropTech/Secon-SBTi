namespace SeconSbtiBackend.DTOs;

public enum PurchasedServiceUpdateStatus
{
    Success,
    ValidationError,
    NotFound
}

public sealed record PurchasedServiceUpdateResult(
    PurchasedServiceUpdateStatus Status,
    PurchasedServiceResponse? Value = null,
    string? Field = null,
    string? Message = null)
{
    public static PurchasedServiceUpdateResult Success(PurchasedServiceResponse value) =>
        new(PurchasedServiceUpdateStatus.Success, Value: value);

    public static PurchasedServiceUpdateResult ValidationError(string field, string message) =>
        new(PurchasedServiceUpdateStatus.ValidationError, Field: field, Message: message);

    public static PurchasedServiceUpdateResult NotFound(string message) =>
        new(PurchasedServiceUpdateStatus.NotFound, Message: message);
}
