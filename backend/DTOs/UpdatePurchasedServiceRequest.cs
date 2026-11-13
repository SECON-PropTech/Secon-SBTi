using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.DTOs;

public sealed class UpdatePurchasedServiceRequest : IValidatableObject
{
    [Required(ErrorMessage = "Ange periodens startdatum.")]
    public DateOnly? PeriodFrom { get; init; }

    [Required(ErrorMessage = "Ange periodens slutdatum.")]
    public DateOnly? PeriodTo { get; init; }

    [Required(ErrorMessage = "Ange ett belopp.")]
    public decimal? Value { get; init; }

    [Required(ErrorMessage = "Ange en enhet.")]
    [RegularExpression("^sek$", ErrorMessage = "Endast belopp i SEK stöds.")]
    public string? Unit { get; init; }

    [Required(ErrorMessage = "Ange om värdet är uppmätt eller uppskattat.")]
    [RegularExpression(
        "^(measured|estimated)$",
        ErrorMessage = "Välj uppmätt eller uppskattat.")]
    public string? MeasurementQuality { get; init; }

    public string? Comment { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (PeriodFrom.HasValue &&
            PeriodTo.HasValue &&
            PeriodTo.Value < PeriodFrom.Value)
        {
            yield return new ValidationResult(
                "Slutdatum får inte vara före startdatum.",
                ["periodTo"]);
        }

        if (Value < 0)
        {
            yield return new ValidationResult(
                "Beloppet måste vara noll eller större.",
                ["value"]);
        }
    }
}
