// tests/DTOsTests/RoleDtosTests.cs
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.DTOs;

namespace tests.DTOsTests;

public class RoleDtosTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static RoleRequest ValidRequest() => new()
    {
        Name = "Admin",
        Description = "Administratör",
        Permissions = [Permission.ManageRoles, Permission.ImportMobigo]
    };

    /******************RoleRequest*******************/
    [Fact]
    public void RoleRequest_Valid_HasNoErrors()
    {
        Validate(ValidRequest()).Should().BeEmpty();
    }

    [Fact]
    public void RoleRequest_DescriptionIsOptional()
    {
        var request = new RoleRequest { Name = "Admin", Permissions = [] };
        Validate(request).Should().BeEmpty();
        request.Description.Should().BeNull();
    }

    [Fact]
    public void RoleRequest_EmptyPermissionList_IsValid()
    {
        Validate(new RoleRequest { Name = "Admin", Permissions = [] }).Should().BeEmpty();
    }

    [Fact]
    public void RoleRequest_MissingName_IsInvalid()
    {
        var request = new RoleRequest { Permissions = [Permission.ManageRoles] };
        var results = Validate(request);

        results.SelectMany(r => r.MemberNames).Should().Equal(nameof(RoleRequest.Name));
        results.Single().ErrorMessage.Should().Be("Ange ett namn.");
    }

    [Fact]
    public void RoleRequest_MissingPermissions_IsInvalid()
    {
        var request = new RoleRequest { Name = "Admin" };
        var results = Validate(request);

        results.SelectMany(r => r.MemberNames).Should().Equal(nameof(RoleRequest.Permissions));
        results.Single().ErrorMessage.Should().Be("Ange rollens behörigheter.");
    }

    [Fact]
    public void RoleRequest_UnknownPermission_IsInvalid()
    {
        var request = new RoleRequest
        {
            Name = "Admin",
            Permissions = [Permission.ManageRoles, (Permission)999]
        };

        var results = Validate(request);
        var error = results.Should().ContainSingle().Subject;
        error.ErrorMessage.Should().Be("Okänd behörighet.");
        error.MemberNames.Should().Equal("permissions");
    }

    [Fact]
    public void RoleRequest_Validate_WithNullPermissions_ReturnsNoErrors()
    {
        var request = new RoleRequest { Name = "Admin", Permissions = null };
        request.Validate(new ValidationContext(request)).Should().BeEmpty();
    }

    [Fact]
    public void RoleRequest_Validate_WithOnlyKnownPermissions_ReturnsNoErrors()
    {
        var request = ValidRequest();
        request.Validate(new ValidationContext(request)).Should().BeEmpty();
    }

    /******************UserRolesRequest*******************/
    [Fact]
    public void UserRolesRequest_Valid_HasNoErrors()
    {
        var request = new UserRolesRequest { RoleIds = [Guid.NewGuid()] };
        Validate(request).Should().BeEmpty();
        request.RoleIds.Should().HaveCount(1);
    }

    [Fact]
    public void UserRolesRequest_EmptyList_IsValid()
    {
        Validate(new UserRolesRequest { RoleIds = [] }).Should().BeEmpty();
    }

    [Fact]
    public void UserRolesRequest_MissingRoleIds_IsInvalid()
    {
        var results = Validate(new UserRolesRequest());
        results.SelectMany(r => r.MemberNames).Should().Equal(nameof(UserRolesRequest.RoleIds));
        results.Single().ErrorMessage.Should().Be("Ange användarens roller.");
    }

    /******************RoleResponse*******************/
    [Fact]
    public void RoleResponse_ExposesAllValues()
    {
        var id = Guid.NewGuid();

        var response = new RoleResponse(id, "Admin", "Administratör", [Permission.ManageRoles], 3);

        response.Id.Should().Be(id);
        response.Name.Should().Be("Admin");
        response.Description.Should().Be("Administratör");
        response.Permissions.Should().Equal(Permission.ManageRoles);
        response.UserCount.Should().Be(3);
    }

    [Fact]
    public void RoleResponse_RecordMembers_Work()
    {
        var id = Guid.NewGuid();
        var permissions = new List<Permission> { Permission.ManageRoles };
        var response = new RoleResponse(id, "Admin", null, permissions, 1);
        var same = new RoleResponse(id, "Admin", null, permissions, 1);
        var changed = response with { Name = "Annan" };
        var (deconstructedId, name, description, perms, count) = response;

        response.Should().Be(same);
        response.GetHashCode().Should().Be(same.GetHashCode());
        response.Equals((object)same).Should().BeTrue();
        changed.Should().NotBe(response);
        changed.Name.Should().Be("Annan");
        response.ToString().Should().Contain("Admin");
        (deconstructedId, name, description, perms, count).Should().Be((id, "Admin", null, permissions, 1));
    }

    /******************UserWithRolesResponse*******************/

    [Fact]
    public void UserWithRolesResponse_ExposesAllValues()
    {
        var id = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var response = new UserWithRolesResponse(id, "Anna", "Testsson", "anna@secon.se", true, [roleId]);

        response.Id.Should().Be(id);
        response.FirstName.Should().Be("Anna");
        response.LastName.Should().Be("Testsson");
        response.Email.Should().Be("anna@secon.se");
        response.IsActivated.Should().BeTrue();
        response.RoleIds.Should().Equal(roleId);
    }

    [Fact]
    public void UserWithRolesResponse_RecordMembers_Work()
    {
        var id = Guid.NewGuid();
        var roleIds = new List<Guid> { Guid.NewGuid() };
        var response = new UserWithRolesResponse(id, "Anna", "Testsson", null, false, roleIds);
        var same = new UserWithRolesResponse(id, "Anna", "Testsson", null, false, roleIds);
        var changed = response with { IsActivated = true };
        var (deconstructedId, first, last, email, activated, ids) = response;

        response.Should().Be(same);
        response.GetHashCode().Should().Be(same.GetHashCode());
        response.Equals((object)same).Should().BeTrue();
        changed.Should().NotBe(response);
        changed.IsActivated.Should().BeTrue();
        response.ToString().Should().Contain("Anna");
        (deconstructedId, first, last, email, activated, ids).Should().Be((id, "Anna", "Testsson", null, false, roleIds));
    }
}