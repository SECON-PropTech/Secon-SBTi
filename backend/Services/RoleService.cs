using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Services;

public class RoleService(ApplicationDbContext dbContext) : IRoleService
{
    public Task<List<RoleResponse>> GetAllAsync() => ToResponsesAsync(dbContext.Roles);

    public async Task<RoleResponse?> GetAsync(Guid id)
    {
        var roles = await ToResponsesAsync(dbContext.Roles.Where(role => role.Id == id));

        return roles.SingleOrDefault();
    }

    public async Task<RoleResponse?> CreateAsync(RoleRequest request)
    {
        if (await IsNameTakenAsync(request.Name!))
            return null;

        var role = new Role();
        Apply(role, request);

        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        return await GetAsync(role.Id);
    }

    public async Task<RoleOutcome> UpdateAsync(Guid id, RoleRequest request)
    {
        var role = await dbContext.Roles
            .Include(role => role.RolePermissions)
            .FirstOrDefaultAsync(role => role.Id == id);

        if (role is null)
            return RoleOutcome.NotFound;

        if (await IsNameTakenAsync(request.Name!, exceptId: id))
            return RoleOutcome.NameTaken;

        Apply(role, request);
        await dbContext.SaveChangesAsync();

        return RoleOutcome.Ok;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deletedCount = await dbContext.Roles
            .Where(role => role.Id == id)
            .ExecuteDeleteAsync();

        return deletedCount > 0;
    }

    public async Task<List<UserWithRolesResponse>> GetUsersAsync()
    {
        return await dbContext.AppUsers
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new UserWithRolesResponse(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PasswordHash != null,
                user.Roles.Select(role => role.Id).ToList()))
            .ToListAsync();
    }

    public async Task<RoleOutcome> SetUserRolesAsync(Guid userId, UserRolesRequest request)
    {
        var user = await dbContext.AppUsers
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(user => user.Id == userId);

        if (user is null)
            return RoleOutcome.NotFound;

        var roleIds = request.RoleIds!.Distinct().ToList();
        var roles = await dbContext.Roles
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync();

        if (roles.Count != roleIds.Count)
            return RoleOutcome.UnknownRole;

        user.Roles.Clear();
        foreach (var role in roles)
        {
            user.Roles.Add(role);
        }

        await dbContext.SaveChangesAsync();

        return RoleOutcome.Ok;
    }

    private void Apply(Role role, RoleRequest request)
    {
        role.Name = request.Name!.Trim();
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        var wanted = request.Permissions!.Select(permission => permission.ToString()).ToHashSet();
        var current = role.RolePermissions.Select(rolePermission => rolePermission.Permission).ToHashSet();

        dbContext.RolePermissions.RemoveRange(
            role.RolePermissions.Where(rolePermission => !wanted.Contains(rolePermission.Permission)).ToList());

        foreach (var permission in wanted.Except(current))
        {
            role.RolePermissions.Add(new RolePermission { Permission = permission });
        }
    }

    private Task<bool> IsNameTakenAsync(string name, Guid? exceptId = null)
    {
        var trimmed = name.Trim();

        return dbContext.Roles.AnyAsync(role => role.Name == trimmed && role.Id != exceptId);
    }

    private static async Task<List<RoleResponse>> ToResponsesAsync(IQueryable<Role> roles)
    {
        var rows = await roles
            .OrderBy(role => role.Name)
            .Select(role => new
            {
                role.Id,
                role.Name,
                role.Description,
                PermissionNames = role.RolePermissions.Select(rolePermission => rolePermission.Permission).ToList(),
                UserCount = role.Users.Count
            })
            .ToListAsync();

        return rows
            .Select(row => new RoleResponse(row.Id, row.Name, row.Description, ToPermissions(row.PermissionNames), row.UserCount))
            .ToList();
    }

    private static List<Permission> ToPermissions(List<string> names) =>
        Enum.GetValues<Permission>()
            .Where(permission => names.Contains(permission.ToString()))
            .ToList();
}
