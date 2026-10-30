using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;

using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDTO>> Register(RegisterRequestDTO request)
    {
        var user = await authService.RegisterAsync(request);
        if(user is null)
            return Conflict(new { message = "Det finns redan ett konto med den e-postadressen."});

        return StatusCode(StatusCodes.Status201Created, ToDto(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> Login([FromBody] LoginRequestDTO request)
    {
        var user = await authService.LoginAsync(request);
        if(user is null)
        {
            return Unauthorized(new
            {
                message = "Felaktig e-postadress eller lösenord."
            });
        }
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return Ok(ToDto(user));
        
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

    private static UserDTO ToDto(AppUser u) => new()
    {
        Id = u.Id,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email

    };
}