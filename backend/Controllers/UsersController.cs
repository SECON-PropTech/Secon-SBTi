using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/users")]
[HasPermission(Permission.ManageRoles)]
public class UsersController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    public async Task<List<UserWithRolesResponse>> GetAll() => await roleService.GetUsersAsync();

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> SetRoles(Guid id, UserRolesRequest request)
    {
        var outcome = await roleService.SetUserRolesAsync(id, request);

        return outcome switch
        {
            RoleOutcome.Ok => NoContent(),
            RoleOutcome.NotFound => NotFound(),
            RoleOutcome.UnknownRole => this.FieldValidationProblem("roleIds", "En eller flera roller finns inte."),
            RoleOutcome.LastRoleManager => Conflict(new { message = "Minst en användare måste kunna hantera roller." }),
            _ => throw new InvalidOperationException($"Unexpected outcome: {outcome}")
        };
    }
}
