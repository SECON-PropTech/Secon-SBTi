using SeconSbtiBackend.DTOs;

namespace SeconSbtiBackend.Interfaces;

public enum RoleOutcome
{
    Ok,
    NotFound,
    NameTaken,
    UnknownRole,
    LastRoleManager
}

public interface IRoleService
{
    Task<List<RoleResponse>> GetAllAsync();

    Task<RoleResponse?> GetAsync(Guid id);

    Task<RoleResponse?> CreateAsync(RoleRequest request);

    Task<RoleOutcome> UpdateAsync(Guid id, RoleRequest request);

    Task<RoleOutcome> DeleteAsync(Guid id);

    Task<List<UserWithRolesResponse>> GetUsersAsync();

    Task<RoleOutcome> SetUserRolesAsync(Guid userId, UserRolesRequest request);
}
