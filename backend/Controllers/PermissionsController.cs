using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Authorization;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    [HasPermission(Permission.ManageRoles)]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public List<Permission> GetAll()
    {
        return Enum.GetValues<Permission>().ToList();
    }
}
