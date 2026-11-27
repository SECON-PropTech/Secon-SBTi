using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.Validation;

/// <summary>
/// Accepts only whole numbers, e.g. 3 but not 3.5. Null passes so [Required] decides about missing values.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class WholeNumberAttribute : ValidationAttribute
{
    public WholeNumberAttribute()
        : base("Ange ett helt antal.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is null || value is decimal number && number % 1 == 0;
    }
}
