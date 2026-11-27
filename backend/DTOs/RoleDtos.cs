using System.ComponentModel.DataAnnotations;
using SeconSbtiBackend.Authorization;

namespace SeconSbtiBackend.DTOs;

public class RoleRequest : IValidatableObject
{
    [Required(ErrorMessage = "Ange ett namn.")]
    public string? Name { get; init; }

    public string? Description { get; init; }

    [Required(ErrorMessage = "Ange rollens behörigheter.")]
    public List<Permission>? Permissions { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Permissions is not null && Permissions.Any(permission => !Enum.IsDefined(permission)))
        {
            yield return new ValidationResult("Okänd behörighet.", ["permissions"]);
        }
    }
}

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    List<Permission> Permissions,
    int UserCount);

public class UserRolesRequest
{
    [Required(ErrorMessage = "Ange användarens roller.")]
    public List<Guid>? RoleIds { get; init; }
}

public record UserWithRolesResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    bool IsActivated,
    List<Guid> RoleIds);
