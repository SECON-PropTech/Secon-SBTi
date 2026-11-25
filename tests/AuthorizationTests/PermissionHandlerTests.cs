using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Interfaces;
using Xunit;

namespace tests.AuthorizationTests;

public class PermissionHandlerTests
{
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();
    private readonly PermissionHandler _handler;

    public PermissionHandlerTests()
    {
        _handler = new PermissionHandler(_permissionService);
    }

    private static ClaimsPrincipal UserWithId(Guid id) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], authenticationType: "Test"));

    private static ClaimsPrincipal UserWithRawIdClaim(string value) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, value)], authenticationType: "Test"));

    private static AuthorizationHandlerContext CreateContext(
        ClaimsPrincipal user,
        params HasPermissionAttribute[] requirements) => new(requirements, user, resource: null);


    /****************************Granted*****************************/
    [Fact]
    public async Task HandleAsync_WhenUserHasPermission_Succeeds()
    {
        var userId = Guid.NewGuid();
        var requirement = new HasPermissionAttribute(Permission.ManageRoles);
        var context = CreateContext(UserWithId(userId), requirement);
        _permissionService.HasPermissionAsync(userId, Permission.ManageRoles).Returns(true);
        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        context.HasFailed.Should().BeFalse();
        context.PendingRequirements.Should().BeEmpty();
    }

    [Theory]
    [InlineData(Permission.ImportVisma4010)]
    [InlineData(Permission.ImportMobigo)]
    [InlineData(Permission.ManageRoles)]
    public async Task HandleAsync_AsksServiceForUserIdAndRequiredPermission(Permission permission)
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(UserWithId(userId), new HasPermissionAttribute(permission));
        _permissionService.HasPermissionAsync(userId, permission).Returns(true);

        await _handler.HandleAsync(context);
        await _permissionService.Received(1).HasPermissionAsync(userId, permission);
    }

    /************************Denied************************/
    [Fact]
    public async Task HandleAsync_WhenUserLacksPermission_DoesNotSucceed()
    {
        var userId = Guid.NewGuid();
        var requirement = new HasPermissionAttribute(Permission.ManageRoles);
        var context = CreateContext(UserWithId(userId), requirement);
        _permissionService.HasPermissionAsync(userId, Permission.ManageRoles).Returns(false);
        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.PendingRequirements.Should().Contain(requirement);
    }

    [Fact]
    public async Task HandleAsync_WhenUserLacksPermission_DoesNotExplicitlyFail()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(UserWithId(userId), new HasPermissionAttribute(Permission.ManageRoles));
        _permissionService.HasPermissionAsync(userId, Permission.ManageRoles).Returns(false);
        await _handler.HandleAsync(context);
        //The handler only ever calls Succeed, so other handlers still get a chance to approve
        context.HasFailed.Should().BeFalse();
    }

    /********************Missing Or Invalid User Id*********************/
    [Fact]
    public async Task HandleAsync_WhenUserHasNoClaims_DoesNotSucceedAndSkipsService()
    {
        var context = CreateContext(new ClaimsPrincipal(), new HasPermissionAttribute(Permission.ManageRoles));
        await _handler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
        await _permissionService.DidNotReceiveWithAnyArgs().HasPermissionAsync(default, default);
    }

    [Fact]
    public async Task HandleAsync_WhenOnlyOtherClaimsExist_DoesNotSucceedAndSkipsService()
    {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Email, "test@example.com")], authenticationType: "Test"));
        var context = CreateContext(user, new HasPermissionAttribute(Permission.ManageRoles));
        await _handler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
        await _permissionService.DidNotReceiveWithAnyArgs().HasPermissionAsync(default, default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    public async Task HandleAsync_WhenUserIdClaimIsNotAGuid_DoesNotSucceedAndSkipsService(string claimValue)
    {
        var context = CreateContext(UserWithRawIdClaim(claimValue), new HasPermissionAttribute(Permission.ManageRoles));
        await _handler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
        await _permissionService.DidNotReceiveWithAnyArgs().HasPermissionAsync(default, default);
    }

    /************************Multiple Requirements***********************/

    [Fact]
    public async Task HandleAsync_WithTwoRequirements_OnlySucceedsTheOnesTheUserHas()
    {
        var userId = Guid.NewGuid();
        var mobigo = new HasPermissionAttribute(Permission.ImportMobigo);
        var manageRoles = new HasPermissionAttribute(Permission.ManageRoles);
        var context = CreateContext(UserWithId(userId), mobigo, manageRoles);
        _permissionService.HasPermissionAsync(userId, Permission.ImportMobigo).Returns(true);
        _permissionService.HasPermissionAsync(userId, Permission.ManageRoles).Returns(false);
        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("all requirements must be met");
        context.PendingRequirements.Should().ContainSingle().Which.Should().Be(manageRoles);
    }

    [Fact]
    public async Task HandleAsync_WithTwoRequirements_SucceedsWhenUserHasBoth()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(
            UserWithId(userId),
            new HasPermissionAttribute(Permission.ImportVisma4010),
            new HasPermissionAttribute(Permission.ImportMobigo));
            
        _permissionService.HasPermissionAsync(userId, Arg.Any<Permission>()).Returns(true);
        await _handler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }
}