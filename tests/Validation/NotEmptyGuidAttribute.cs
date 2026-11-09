using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute()
        : base("Ange ett giltigt ID som inte är tomt.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is Guid id && id != Guid.Empty;
    }
}
