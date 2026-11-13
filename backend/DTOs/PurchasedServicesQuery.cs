using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.DTOs;

public sealed class PurchasedServicesQuery : IValidatableObject
{
    [Range(1, 9999, ErrorMessage = "Ange ett år mellan 1 och 9999.")]
    public int? Year { get; init; }

    [Range(1, 12, ErrorMessage = "Ange en månad mellan 1 och 12.")]
    public int? Month { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Month.HasValue && !Year.HasValue)
        {
            yield return new ValidationResult(
                "Ange ett år när du filtrerar på månad.",
                ["year"]);
        }
    }
}