namespace SeconSbtiBackend.DTOs;

/// <summary>
/// One delivered quantity of a sold product, for GET /api/material-lines (FK-DI.02).
/// Only lines with IsManual = true can be changed or deleted with PUT and DELETE.
/// </summary>
public record MaterialLineResponse(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    DateOnly Date,
    string MeasurementQuality,
    string? Comment,
    bool IsManual);