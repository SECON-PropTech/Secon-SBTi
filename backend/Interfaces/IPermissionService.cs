using SeconSbtiBackend.Authorization;

namespace SeconSbtiBackend.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Permission permission);
}
