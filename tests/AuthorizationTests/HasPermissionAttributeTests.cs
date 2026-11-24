using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using SeconSbtiBackend.Authorization;
using Xunit;

namespace tests.AuthorizationTests;

public class HasPermissionAttributeTests
{
    [Theory]
    [InlineData(Permission.ImportVisma4010)]
    [InlineData(Permission.ImportMobigo)]
    [InlineData(Permission.ManageRoles)]
    public void Constructor_StoresPermission(Permission permission)
    {
        var attribute = new HasPermissionAttribute(permission);
        attribute.Permission.Should().Be(permission);
    }

    [Fact]
    public void GetRequirements_ReturnsExactlyItself()
    {
        var attribute = new HasPermissionAttribute(Permission.ManageRoles);
        var requirements = attribute.GetRequirements().ToList();
        requirements.Should().ContainSingle();
        requirements[0].Should().BeSameAs(attribute);
    }

    [Fact]
    public void GetRequirements_ReturnedRequirementCarriesThePermission()
    {
        var attribute = new HasPermissionAttribute(Permission.ImportMobigo);
        var requirement = attribute.GetRequirements().Single();
        requirement.Should().BeOfType<HasPermissionAttribute>()
            .Which.Permission.Should().Be(Permission.ImportMobigo);
    }

    [Fact]
    public void Attribute_ImplementsTheTypesNeededByTheAuthorizationSystem()
    {
        var attribute = new HasPermissionAttribute(Permission.ManageRoles);
        attribute.Should().BeAssignableTo<AuthorizeAttribute>();
        attribute.Should().BeAssignableTo<IAuthorizationRequirement>();
        attribute.Should().BeAssignableTo<IAuthorizationRequirementData>();
    }

    [Fact]
    public void Attribute_CanBeAppliedToClassesAndMethods_AndMoreThanOnce()
    {
        var usage = typeof(HasPermissionAttribute).GetCustomAttribute<AttributeUsageAttribute>(inherit: true);
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Method);
        usage.AllowMultiple.Should().BeTrue();
    }
}