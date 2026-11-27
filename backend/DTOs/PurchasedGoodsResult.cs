namespace SeconSbtiBackend.DTOs;

public enum PurchasedGoodsResultStatus
{
    Success,
    ValidationError,
    NotFound,
    InvalidUser
}

public sealed record PurchasedGoodsResult<T>(
    PurchasedGoodsResultStatus Status,
    T? Value = default,
    string? Field = null,
    string? Message = null)
{
    public static PurchasedGoodsResult<T> Success(T value) =>
        new(PurchasedGoodsResultStatus.Success, Value: value);

    public static PurchasedGoodsResult<T> ValidationError(string field, string message) =>
        new(PurchasedGoodsResultStatus.ValidationError, Field: field, Message: message);

    public static PurchasedGoodsResult<T> NotFound(string message) =>
        new(PurchasedGoodsResultStatus.NotFound, Message: message);

    public static PurchasedGoodsResult<T> InvalidUser(string message) =>
        new(PurchasedGoodsResultStatus.InvalidUser, Message: message);
}
