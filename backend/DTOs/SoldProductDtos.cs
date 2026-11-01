using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.DTOs;

/// <summary>
/// Request body for PUT /api/products/{id}: manual input of a sold product's data (FK-DI.02, FK-AB.3).
/// Null means "not set" and is not the same as 0 (IF-SG.03).
/// </summary>
public record UpdateSoldProductRequest
{
    /// <summary>
    /// Product group the product is reported in. Null = no group yet.
    /// </summary>
    public Guid? GroupId { get; init; }

    /// <summary>
    /// Energy use per year in kWh. 0 is allowed (product uses no electricity).
    /// </summary>
    [Range(0d, double.MaxValue)]
    public decimal? KwhPerYear { get; init; }

    /// <summary>
    /// Lifetime in years. Must be above 0, same rule as the database.
    /// </summary>
    [Range(0d, double.MaxValue, MinimumIsExclusive = true)]
    public decimal? LifetimeYears { get; init; }

    /// <summary>
    /// "measured" or "estimated" (FK-DI.08).
    /// </summary>
    [AllowedValues("measured", "estimated")]
    public required string MeasurementQuality { get; init; }

    /// <summary>
    /// Free text, e.g. where an estimate comes from (FK-DI.08).
    /// </summary>
    public string? Comment { get; init; }
}