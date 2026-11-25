using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.DTOs.Scope312;

public sealed class UpdateProductMaterialWeightRequest
{
    [Required(ErrorMessage = "Ange materialets vikt i kg.")]
    [Range(
        typeof(decimal),
        "0",
        "79228162514264337593543950335",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Vikten måste vara minst 0 kg.")]
    public decimal? WeightKg { get; init; }
}