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
    /// Whole numbers only: products are counted in pieces ("st"), as in the Mobigo export.
    /// </summary>
    [Required(ErrorMessage = "Ange ett antal.")]
    [Range(0d, 1_000_000_000d, ErrorMessage = "Ange ett antal mellan 0 och 1 000 000 000.")]
    [WholeNumber(ErrorMessage = "Ange ett helt antal, till exempel 3.")]
    public decimal? Quantity { get; init; }

    /// <summary>
    /// Delivery date. Decides which reporting year the line counts in.
    /// The year must be 2000–2100, the same range GET /api/material-lines accepts.
    /// </summary>
    [Required(ErrorMessage = "Ange ett leveransdatum.")]
    [Range(typeof(DateOnly), "2000-01-01", "2100-12-31",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Ange ett datum med ett år mellan 2000 och 2100.")]
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