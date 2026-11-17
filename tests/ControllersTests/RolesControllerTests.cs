using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using Xunit;

namespace tests.ControllersTests;

public class RolesControllerTests
{
    private const string NameTakenMessage = "Det finns redan en roll med det namnet.";
    private const string LastRoleManagerMessage = "Minst en användare måste kunna hantera roller.";
    private readonly IRoleService _roleService = Substitute.For<IRoleService>();
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _controller = new RolesController(_roleService);
    }

    private static RoleRequest CreateRequest() => new()
    {
        Name = "Admin",
        Description = "Administratör",
        Permissions = [Permission.ManageRoles]
    };

    private static RoleResponse CreateResponse(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Admin", "Administratör", [Permission.ManageRoles], 1);


    /**************************Get Roles******************************/
    [Fact]
    public async Task GetAll_ReturnsRolesFromService()
    {
        var roles = new List<RoleResponse> { CreateResponse(), CreateResponse() };
        _roleService.GetAllAsync().Returns(roles);
        var result = await _controller.GetAll();
        result.Should().BeSameAs(roles);
    }

    [Fact]
    public async Task GetAll_WhenNoRoles_ReturnsEmptyList()
    {
        _roleService.GetAllAsync().Returns(new List<RoleResponse>());
        var result = await _controller.GetAll();
        result.Should().BeEmpty();
    }


    [Fact]
    public async Task Get_WhenRoleExists_ReturnsOkWithRole()
    {
        var role = CreateResponse();
        _roleService.GetAsync(role.Id).Returns(role);
        var result = await _controller.Get(role.Id);
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(role);
    }

    [Fact]
    public async Task Get_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _roleService.GetAsync(id).Returns(Task.FromResult<RoleResponse?>(null));

        var result = await _controller.Get(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /************************Create Roles**************************/
    [Fact]
    public async Task Create_WhenSuccessful_Returns201WithRole()
    {
        var request = CreateRequest();
        var role = CreateResponse();
        _roleService.CreateAsync(request).Returns(role);
        var result = await _controller.Create(request);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().Be(role);
    }

    [Fact]
    public async Task Create_WhenNameIsTaken_ReturnsConflictWithMessage()
    {
        var request = CreateRequest();
        _roleService.CreateAsync(request).Returns(Task.FromResult<RoleResponse?>(null));
        var result = await _controller.Create(request);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new { message = NameTakenMessage });
    }

    [Fact]
    public async Task Create_PassesRequestToService()
    {
        var request = CreateRequest();
        _roleService.CreateAsync(request).Returns(CreateResponse());
        await _controller.Create(request);
        await _roleService.Received(1).CreateAsync(request);
    }

    /************************Update Roles***********************/
    [Fact]
    public async Task Update_WhenOk_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.UpdateAsync(id, request).Returns(RoleOutcome.Ok);
        var result = await _controller.Update(id, request);

        result.Should().BeOfType<NoContentResult>();
        await _roleService.Received(1).UpdateAsync(id, request);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.UpdateAsync(id, request).Returns(RoleOutcome.NotFound);
        var result = await _controller.Update(id, request);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(RoleOutcome.NameTaken, NameTakenMessage)]
    [InlineData(RoleOutcome.LastRoleManager, LastRoleManagerMessage)]
    public async Task Update_OnConflictingOutcome_ReturnsConflictWithMessage(RoleOutcome outcome, string expectedMessage)
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.UpdateAsync(id, request).Returns(outcome);
        var result = await _controller.Update(id, request);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new { message = expectedMessage });
    }

    [Fact]
    public async Task Update_WhenOutcomeIsUnexpected_Throws()
    {
        var id = Guid.NewGuid();
        var request = CreateRequest();
        _roleService.UpdateAsync(id, request).Returns(RoleOutcome.UnknownRole);

        var act = () => _controller.Update(id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*UnknownRole*");
    }

    /***********************Delete Roles***************************/

    [Fact]
    public async Task Delete_WhenOk_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _roleService.DeleteAsync(id).Returns(RoleOutcome.Ok);
        var result = await _controller.Delete(id);
        result.Should().BeOfType<NoContentResult>();
        await _roleService.Received(1).DeleteAsync(id);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _roleService.DeleteAsync(id).Returns(RoleOutcome.NotFound);
        var result = await _controller.Delete(id);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_WhenLastRoleManager_ReturnsConflictWithMessage()
    {
        var id = Guid.NewGuid();
        _roleService.DeleteAsync(id).Returns(RoleOutcome.LastRoleManager);
        var result = await _controller.Delete(id);
        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new { message = LastRoleManagerMessage });
    }

    [Theory]
    [InlineData(RoleOutcome.NameTaken)]
    [InlineData(RoleOutcome.UnknownRole)]
    public async Task Delete_WhenOutcomeIsUnexpected_Throws(RoleOutcome outcome)
    {
        var id = Guid.NewGuid();
        _roleService.DeleteAsync(id).Returns(outcome);
        var act = () => _controller.Delete(id);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /************************Routing*************************/

    [Fact]
    public void Controller_IsApiControllerWithExpectedRoute()
    {
        var type = typeof(RolesController);
        type.Should().BeDecoratedWith<ApiControllerAttribute>();
        type.Should().BeDecoratedWith<RouteAttribute>(attribute => attribute.Template == "api/roles");
    }

    [Fact]
    public void Controller_RequiresManageRolesPermission()
    {
        typeof(RolesController).Should().BeDecoratedWith<HasPermissionAttribute>(
            attribute => attribute.Permission == Permission.ManageRoles);
    }
}