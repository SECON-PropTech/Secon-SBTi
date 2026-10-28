using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.API.Dtos;
using SeconSbtiBackend.Core.Interfaces;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequestDto request)
    {
        var user = await authService.RegisterAsync(request);
        if(user is null)
            return Conflict(new { message = "Det finns redan ett konto med den e-postadressen."});

        return StatusCode(StatusCodes.Status201Created, ToDto(user));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        name = User.Identity?.Name,
        email = User.FindFirstValue(ClaimTypes.Email),
        roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
    });

    private static UserDto ToDto(AppUser u) => new()
    {
        Id = u.Id,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email

    };
}