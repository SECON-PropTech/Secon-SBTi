using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.DTOs.Scope312;

// Innehåller uppgifterna som kan ändras på en manuell registrering.
public sealed class UpdateProductPeriodEntryRequest
{
    [Required(ErrorMessage = "Ange ett antal.")]
    [Range(0d, 1_000_000_000d, ErrorMessage = "Ange ett antal mellan 0 och 1 000 000 000.")]
    public decimal? Quantity { get; init; }

    [Required(ErrorMessage = "Ange ett leveransdatum.")]
    public DateOnly? Date { get; init; }

    [Required(ErrorMessage = "Ange om antalet är uppmätt eller uppskattat.")]
    [AllowedValues("measured", "estimated", ErrorMessage = "Välj uppmätt eller uppskattat.")]
    public string? MeasurementQuality { get; init; }

    public string? Comment { get; init; }
}