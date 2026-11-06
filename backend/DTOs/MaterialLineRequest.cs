using System.ComponentModel.DataAnnotations;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.DTOs;

/// <summary>
/// Request body for POST and PUT /api/material-lines: a manually registered quantity of a sold product (FK-DI.02).
/// Fields are nullable so a missing value fails [Required] instead of silently becoming 0.
/// </summary>
public class MaterialLineRequest
{
    /// <summary>
    /// The sold product the quantity belongs to. Must not be an empty Guid.
    /// </summary>
    [Required(ErrorMessage = "Välj en produkt.")]
    [NotEmptyGuid(ErrorMessage = "Välj en giltig produkt.")]
    public Guid? ProductId { get; init; }

    /// <summary>
    /// Number of units delivered. 0 is allowed, negative is not (#44).
    /// </summary>
    [Required(ErrorMessage = "Ange ett antal.")]
    [Range(0d, 1_000_000_000d, ErrorMessage = "Antalet får inte vara negativt.")]
    public decimal? Quantity { get; init; }

    /// <summary>
    /// Delivery date. Decides which reporting year the line counts in.
    /// </summary>
    [Required(ErrorMessage = "Ange ett leveransdatum.")]
    public DateOnly? Date { get; init; }

    /// <summary>
    /// "measured" or "estimated", same rule as the database CHECK (FK-DI.08).
    /// </summary>
    [Required(ErrorMessage = "Ange om antalet är uppmätt eller uppskattat.")]
    [AllowedValues("measured", "estimated", ErrorMessage = "Välj uppmätt eller uppskattat.")]
    public string? MeasurementQuality { get; init; }

    /// <summary>
    /// Free text, e.g. where an estimate comes from (FK-DI.08). Optional.
    /// </summary>
    public string? Comment { get; init; }
}