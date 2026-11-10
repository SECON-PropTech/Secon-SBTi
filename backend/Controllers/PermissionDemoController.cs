using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Authorization;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/permission-demo")]
public class PermissionDemoController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult Public()
    {
        return Ok(new { message = "Öppen för alla." });
    }

    [Authorize]
    [HttpGet("logged-in")]
    public IActionResult LoggedIn()
    {
        return Ok(new { message = "Du är inloggad." });
    }

    [HasPermission(Permission.ImportVisma4010)]
    [HttpGet("visma")]
    public IActionResult Visma()
    {
        return Ok(new { message = "Du får importera från Visma 4010." });
    }

    [HasPermission(Permission.ImportMobigo)]
    [HttpGet("mobigo")]
    public IActionResult Mobigo()
    {
        return Ok(new { message = "Du får importera från Mobigo." });
    }

    [HasPermission(Permission.ImportVisma4010)]
    [HasPermission(Permission.ImportMobigo)]
    [HttpGet("visma-and-mobigo")]
    public IActionResult VismaAndMobigo()
    {
        return Ok(new { message = "Du får importera från både Visma 4010 och Mobigo." });
    }

    [HasPermission(Permission.ManageRoles)]
    [HttpGet("manage-roles")]
    public IActionResult ManageRoles()
    {
        return Ok(new { message = "Du får hanetra roller." });
    }
}
