using SeconSbtiBackend.Authorization;

namespace SeconSbtiBackend.DTOs;

/// <summary>
/// The logged-in user.
/// </summary>
public class CurrentUserDTO
{
    public Guid Id {get; set;}
    public string Name {get; set;} = string.Empty;
    public string? Email {get; set;}
    public List<string> Roles {get; set;} = [];
    public List<Permission> Permissions {get; set;} = [];
}
