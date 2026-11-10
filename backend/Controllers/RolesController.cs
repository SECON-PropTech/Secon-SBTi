using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/roles")]
[HasPermission(Permission.ManageRoles)]
public class RolesController(IRoleService roleService) : ControllerBase
{
    private const string NameTakenMessage = "Det finns redan en roll med det namnet.";
    private const string LastRoleManagerMessage = "Minst en användare måste kunna hantera roller.";

    [HttpGet]
    public async Task<List<RoleResponse>> GetAll() => await roleService.GetAllAsync();

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponse>> Get(Guid id)
    {
        var role = await roleService.GetAsync(id);

        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleResponse>> Create(RoleRequest request)
    {
        var role = await roleService.CreateAsync(request);

        if (role is null)
            return Conflict(new { message = NameTakenMessage });

        return StatusCode(StatusCodes.Status201Created, role);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, RoleRequest request)
    {
        var outcome = await roleService.UpdateAsync(id, request);

        return outcome switch
        {
            RoleOutcome.Ok => NoContent(),
            RoleOutcome.NotFound => NotFound(),
            RoleOutcome.NameTaken => Conflict(new { message = NameTakenMessage }),
            RoleOutcome.LastRoleManager => Conflict(new { message = LastRoleManagerMessage }),
            _ => throw new InvalidOperationException($"Unexpected outcome: {outcome}")
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var outcome = await roleService.DeleteAsync(id);

        return outcome switch
        {
            RoleOutcome.Ok => NoContent(),
            RoleOutcome.NotFound => NotFound(),
            RoleOutcome.LastRoleManager => Conflict(new { message = LastRoleManagerMessage }),
            _ => throw new InvalidOperationException($"Unexpected outcome: {outcome}")
        };
    }
}
