using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Controllers;
using Xunit;

namespace tests.ControllersTests;

public class PermissionsControllerTests
{
    private readonly PermissionsController _controller = new();
    private static MethodInfo GetAllMethod =>
        typeof(PermissionsController).GetMethod(nameof(PermissionsController.GetAll))!;


    [Fact]
    public void GetAll_ReturnsEveryPermissionInTheEnum()
    {
        var result = _controller.GetAll();
        result.Should().BeEquivalentTo(Enum.GetValues<Permission>());
    }

    [Fact]
    public void GetAll_ReturnsNoDuplicates()
    {
        var result = _controller.GetAll();
        result.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(Permission.ImportVisma4010)]
    [InlineData(Permission.ImportMobigo)]
    [InlineData(Permission.ManageRoles)]
    public void GetAll_ContainsKnownPermission(Permission expected)
    {
        var result = _controller.GetAll();
        result.Should().Contain(expected);
    }

    [Fact]
    public void GetAll_ReturnsNewListOnEachCall()
    {
        var first = _controller.GetAll();
        var second = _controller.GetAll();
        first.Clear();
        second.Should().NotBeSameAs(first);
        second.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAll_SerializesPermissionsAsStrings()
    {
        var result = _controller.GetAll();
        var json = JsonSerializer.Serialize(result);
        var names = JsonSerializer.Deserialize<List<string>>(json);
        names.Should().BeEquivalentTo(result.Select(permission => permission.ToString()));
    }


    [Fact]
    public void Controller_IsApiControllerWithExpectedRoute()
    {
        var type = typeof(PermissionsController);
        type.Should().BeDecoratedWith<ApiControllerAttribute>();
        type.Should().BeDecoratedWith<RouteAttribute>(attribute => attribute.Template == "api/permissions");
    }

    [Fact]
    public void GetAll_IsHttpGet()
    {
        GetAllMethod.Should().BeDecoratedWith<HttpGetAttribute>();
    }

    [Fact]
    public void GetAll_RequiresManageRolesPermission()
    {
        GetAllMethod.Should().BeDecoratedWith<HasPermissionAttribute>(
            attribute => attribute.Permission == Permission.ManageRoles);
    }

    [Fact]
    public void GetAll_IsNotAnonymous()
    {
        GetAllMethod.Should().NotBeDecoratedWith<AllowAnonymousAttribute>();
        typeof(PermissionsController).Should().NotBeDecoratedWith<AllowAnonymousAttribute>();
    }

    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    public void GetAll_DocumentsErrorStatusCodes(int statusCode)
    {
        var documented = GetAllMethod
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(attribute => attribute.StatusCode);
        documented.Should().Contain(statusCode);
    }
}