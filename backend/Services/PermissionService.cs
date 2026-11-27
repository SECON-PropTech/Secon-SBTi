using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Interfaces;

namespace SeconSbtiBackend.Services;

public class PermissionService(ApplicationDbContext dbContext) : IPermissionService
{
    /// <summary>
    /// True if any of the user's roles has the permission. Always asks the database.
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, Permission permission)
    {
        var permissionName = permission.ToString();

        return await dbContext.AppUsers
            .Where(user => user.Id == userId)
            .SelectMany(user => user.Roles)
            .SelectMany(role => role.RolePermissions)
            .AnyAsync(rolePermission => rolePermission.Permission == permissionName);
    }
}
