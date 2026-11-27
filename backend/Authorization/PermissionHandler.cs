using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SeconSbtiBackend.Interfaces;

namespace SeconSbtiBackend.Authorization;

public class PermissionHandler(IPermissionService permissionService) : AuthorizationHandler<HasPermissionAttribute>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasPermissionAttribute requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        if (await permissionService.HasPermissionAsync(userId, requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
