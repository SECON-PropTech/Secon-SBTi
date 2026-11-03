using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace api_tests;

public class AuthControllerTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IAuthenticationService _auth = Substitute.For<IAuthenticationService>();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_auth);
        _sut = new AuthController(_authService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext{RequestServices = services.BuildServiceProvider()}
        
            }
        };

    }

    private static AppUser TestUser() => new(){
        Id = Guid.NewGuid(),
        FirstName = "Anna",
        LastName ="Testsson",
        Email = "anna@secon.se"
    };

    private void SetUser(params Claim[] claims){
        var identity = new ClaimsIdentity(claims, "test");
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    /**********************Register*********************/
    [Fact]
    public async Task Register_Success_Return201()
    {
        var user = TestUser();
        _authService.RegisterAsync(Arg.Any<RegisterRequestDTO>()).Returns(user);
        var result = await _sut.Register(new RegisterRequestDTO());

        var response = result.Result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(201);

        var dto = response.Value.Should().BeOfType<UserDTO>().Subject;
        dto.Id.Should().Be(user.Id);
        dto.FirstName.Should().Be(user.FirstName);
        dto.LastName.Should().Be(user.LastName);
        dto.Email.Should().Be(user.Email);
        dto.GetType().GetProperty("PasswordHash").Should().BeNull();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequestDTO>()).Returns((AppUser?)null);
        var result = await _sut.Register(new RegisterRequestDTO());
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    /**********************Login**********************/
    [Fact]
    public async Task Login_ValidCredentials_SignsInWithCookieAndReturnsOk()
    {
        var user = TestUser();
        _authService.LoginAsync(Arg.Any<LoginRequestDTO>()).Returns(user);
        var result = await _sut.Login(new LoginRequestDTO());
        
        result.Result.Should().BeOfType<OkObjectResult>()
        .Which.Value.Should().BeOfType<UserDTO>()
        .Which.Id.Should().Be(user.Id);

        await _auth.Received(1).SignInAsync(
            Arg.Any<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Is<ClaimsPrincipal>(p =>
                p.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id.ToString()
            ),
            Arg.Any<AuthenticationProperties>()
        );
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        _authService.LoginAsync(Arg.Any<LoginRequestDTO>()).Returns((AppUser?)null);
        var result = await _sut.Login(new LoginRequestDTO());
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        await _auth.DidNotReceiveWithAnyArgs().SignInAsync(default!, default, default!, default);
    }

    /**********************Logout**********************/
    [Fact]
    public async Task Logout_SignsOutAndReturnsNoContent()
    {
        var result = await _sut.Logout();
        result.Should().BeOfType<NoContentResult>();
        await _auth.Received(1).SignOutAsync(
            Arg.Any<HttpContext>(), 
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<AuthenticationProperties>()
            );
    }

    /**********************Me**********************/
    [Fact]
    public void Me_ReturnsCurrentUserClaims()
    {
        SetUser(
            new Claim(ClaimTypes.NameIdentifier, "111"),
            new Claim(ClaimTypes.Name, "Anna Testsson"),
            new Claim(ClaimTypes.Email, "anna@secon.se"),
            new Claim(ClaimTypes.Role, "Admin")
        );

        var result = _sut.Me();
        result.Result.Should().BeOfType<OkObjectResult>()
        .Which.Value.Should().BeEquivalentTo(new
        {
            id = "111",
            name = "Anna Testsson",
            email = "anna@secon.se",
            roles = new[] { "Admin" }
        });
    }

}