namespace SeconSbtiBackend.DTOs.Scope312;

// Beskriver en daterad registrering för en produkt.
public sealed record ProductPeriodEntryResponse(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    DateOnly Date,
    string MeasurementQuality,
    string? Comment
);