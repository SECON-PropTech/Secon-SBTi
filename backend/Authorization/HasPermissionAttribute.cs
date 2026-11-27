using Microsoft.AspNetCore.Authorization;

namespace SeconSbtiBackend.Authorization;

public class HasPermissionAttribute(Permission permission)
    : AuthorizeAttribute, IAuthorizationRequirement, IAuthorizationRequirementData
{
    public Permission Permission { get; } = permission;

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return this;
    }
}
