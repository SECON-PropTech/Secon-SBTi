namespace SeconSbtiBackend.DTOs;

/// <summary>
/// One delivered quantity of a sold product, for GET /api/material-lines (FK-DI.02).
/// ALL lines, regardless of manual or imported can be changed/deleted.
/// </summary>
public record MaterialLineResponse(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    DateOnly Date,
    string MeasurementQuality,
    string? Comment,
    bool IsManual);