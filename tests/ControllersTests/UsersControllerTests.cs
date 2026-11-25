using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NSubstitute;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using Xunit;

namespace tests.ControllersTests;

public class UsersControllerTests
{
    private const string LastRoleManagerMessage = "Minst en användare måste kunna hantera roller.";
    private readonly IRoleService _roleService = Substitute.For<IRoleService>();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_roleService)
        {
            //A simple stand-in to control both error message and field name "roleIds"
            ProblemDetailsFactory = CreateProblemDetailsFactory()
        };
    }

    private static ProblemDetailsFactory CreateProblemDetailsFactory()
    {
        var factory = Substitute.For<ProblemDetailsFactory>();
        factory
            .CreateValidationProblemDetails(
                Arg.Any<HttpContext>(),
                Arg.Any<ModelStateDictionary>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>())
            .Returns(call => new ValidationProblemDetails(call.ArgAt<ModelStateDictionary>(1))
            {
                Status = call.ArgAt<int?>(2) ?? StatusCodes.Status400BadRequest,
                Title = call.ArgAt<string?>(3)
            });

        return factory;
    }

    private static UserRolesRequest CreateRequest() => new() { RoleIds = [Guid.NewGuid(), Guid.NewGuid()] };

    /**********************GetAll Users*************************/
    [Fact]
    public async Task GetAll_ReturnsUsersFromService()
    {
        var users = new List<UserWithRolesResponse>
        {
            new(Guid.NewGuid(), "Anna", "Testsson", "anna@secon.com", true, [Guid.NewGuid()]),
            new(Guid.NewGuid(), "Ben", "Testsson", null, false, [])
        };

        _roleService.GetUsersAsync().Returns(users);
        var result = await _controller.GetAll();
        result.Should().BeSameAs(users);
    }

    [Fact]
    public async Task GetAll_WhenNoUsers_ReturnsEmptyList()
    {
        _roleService.GetUsersAsync().Returns(new List<UserWithRolesResponse>());
        var result = await _controller.GetAll();
        result.Should().BeEmpty();
    }

    /*************************SetRoles****************************/
    [Fact]
    public async Task SetRoles_WhenOk_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.SetUserRolesAsync(id, request).Returns(RoleOutcome.Ok);
        var result = await _controller.SetRoles(id, request);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetRoles_PassesIdAndRequestToService()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.SetUserRolesAsync(id, request).Returns(RoleOutcome.Ok);
        await _controller.SetRoles(id, request);
        await _roleService.Received(1).SetUserRolesAsync(id, request);
    }

    [Fact]
    public async Task SetRoles_WhenUserNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.SetUserRolesAsync(id, request).Returns(RoleOutcome.NotFound);
        var result = await _controller.SetRoles(id, request);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SetRoles_WhenRoleIsUnknown_ReturnsValidationProblemOnRoleIds()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.SetUserRolesAsync(id, request).Returns(RoleOutcome.UnknownRole);
        var result = await _controller.SetRoles(id, request);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Title.Should().Be("En eller flera uppgifter behöver rättas.");
        problem.Errors.Should().ContainKey("roleIds");
        problem.Errors["roleIds"].Should().Contain("En eller flera roller finns inte.");
    }

    [Fact]
    public async Task SetRoles_WhenLastRoleManager_ReturnsConflictWithMessage()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.SetUserRolesAsync(id, request).Returns(RoleOutcome.LastRoleManager);
        var result = await _controller.SetRoles(id, request);
        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new { message = LastRoleManagerMessage });
    }

    [Fact]
    public async Task SetRoles_WhenOutcomeIsUnexpected_Throws()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.SetUserRolesAsync(id, request).Returns(RoleOutcome.NameTaken);
        var act = () => _controller.SetRoles(id, request);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*NameTaken*");
    }

    /**************************Routing**************************/
    [Fact]
    public void Controller_IsApiControllerWithExpectedRoute()
    {
        var type = typeof(UsersController);
        type.Should().BeDecoratedWith<ApiControllerAttribute>();
        type.Should().BeDecoratedWith<RouteAttribute>(attribute => attribute.Template == "api/users");
    }

    [Fact]
    public void Controller_RequiresManageRolesPermission()
    {
        typeof(UsersController).Should().BeDecoratedWith<HasPermissionAttribute>(
            attribute => attribute.Permission == Permission.ManageRoles);
    }

    [Fact]
    public void SetRoles_IsHttpPutOnIdRolesRoute()
    {
        var method = typeof(UsersController).GetMethod(nameof(UsersController.SetRoles))!;
        method.Should().BeDecoratedWith<HttpPutAttribute>(attribute => attribute.Template == "{id:guid}/roles");
    }
}