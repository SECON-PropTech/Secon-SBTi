using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace tests.ServicesTests;

/// <summary>
/// Not covered here: DeleteAsync when it actually deletes (Ok / NotFound). It uses ExecuteDeleteAsync,
/// which the InMemory provider does not support. Cover those two outcomes in the integration tests.
/// </summary>
public class RoleServiceTests
{
    private static DbContextOptions<ApplicationDbContext> NewOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static ApplicationDbContext Db() => new ApplicationDbContext(NewOptions());
    private static RoleService CreateSut(ApplicationDbContext db) => new(db);

    /****************Request builders*******************/

    private static RoleRequest CreateRequest(
        string name,
        string? description = null,
        params Permission[] permissions) => new()
    {
        Name = name,
        Description = description,
        Permissions = permissions.ToList()
    };

    private static UserRolesRequest CreateRolesRequest(params Guid[] roleIds) => new()
    {
        RoleIds = roleIds.ToList()
    };

    /****************Seeding helpers*******************/
    // Every seed helper clears the change tracker afterwards, so the service has to
    // load everything from the database instead of reusing tracked entities.

    private static Task<Guid> SeedRoleAsync(
        ApplicationDbContext db,
        string name,
        params Permission[] permissions) =>
        SeedRoleWithRawPermissionsAsync(db, name, permissions.Select(permission => permission.ToString()).ToArray());

    private static async Task<Guid> SeedRoleWithRawPermissionsAsync(
        ApplicationDbContext db,
        string name,
        params string[] permissionNames)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = name };

        foreach (var permissionName in permissionNames)
        {
            role.RolePermissions.Add(new RolePermission { Permission = permissionName });
        }

        db.Roles.Add(role);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return role.Id;
    }

    private static async Task<Guid> SeedUserAsync(
        ApplicationDbContext db,
        string firstName = "Anna",
        string lastName = "Testsson",
        bool activated = true,
        Guid[]? roleIds = null)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = $"{Guid.NewGuid():N}@secon.se",
            PasswordHash = activated ? "hash" : null
        };

        if (roleIds is { Length: > 0 })
        {
            var roles = await db.Roles.Where(role => roleIds.Contains(role.Id)).ToListAsync();
            foreach (var role in roles)
            {
                user.Roles.Add(role);
            }
        }

        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return user.Id;
    }

    /****************Verification helpers*******************/
    // Clear the change tracker first so we read what is actually stored.

    private static async Task<Role> GetRoleAsync(ApplicationDbContext db, Guid roleId)
    {
        db.ChangeTracker.Clear();

        return await db.Roles.Include(role => role.RolePermissions).SingleAsync(role => role.Id == roleId);
    }

    private static async Task<List<string>> GetRolePermissionNamesAsync(ApplicationDbContext db, Guid roleId) =>
        (await GetRoleAsync(db, roleId)).RolePermissions.Select(rolePermission => rolePermission.Permission).ToList();

    private static async Task<List<Guid>> GetUserRoleIdsAsync(ApplicationDbContext db, Guid userId)
    {
        db.ChangeTracker.Clear();

        var user = await db.AppUsers.Include(u => u.Roles).SingleAsync(u => u.Id == userId);

        return user.Roles.Select(role => role.Id).ToList();
    }

    private static async Task<int> CountRolesAsync(ApplicationDbContext db)
    {
        db.ChangeTracker.Clear();

        return await db.Roles.CountAsync();
    }

    /****************GetAllAsync*******************/

    [Fact]
    public async Task GetAllAsync_NoRoles_ReturnsEmptyList()
    {
        await using var db = Db();

        var result = await CreateSut(db).GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRolesSortedByName()
    {
        await using var db = Db();
        await SeedRoleAsync(db, "Charlie");
        await SeedRoleAsync(db, "Alpha");
        await SeedRoleAsync(db, "Bravo");

        var result = await CreateSut(db).GetAllAsync();

        result.Select(role => role.Name).Should().Equal("Alpha", "Bravo", "Charlie");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPermissionsInEnumOrderAndUserCount()
    {
        await using var db = Db();
        var roleId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles, Permission.ImportVisma4010);
        await SeedUserAsync(db, "Anna", "Andersson", roleIds: [roleId]);
        await SeedUserAsync(db, "Bo", "Berg", roleIds: [roleId]);

        var result = await CreateSut(db).GetAllAsync();

        var role = result.Should().ContainSingle().Subject;
        role.Permissions.Should().Equal(Permission.ImportVisma4010, Permission.ManageRoles);
        role.UserCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_StoredPermissionNameNotInEnum_IsIgnored()
    {
        await using var db = Db();
        await SeedRoleWithRawPermissionsAsync(db, "Legacy", "ManageRoles", "RemovedPermission");

        var result = await CreateSut(db).GetAllAsync();

        result.Single().Permissions.Should().Equal(Permission.ManageRoles);
    }

    /****************GetAsync*******************/

    [Fact]
    public async Task GetAsync_RoleExists_ReturnsOnlyThatRole()
    {
        await using var db = Db();
        var wantedId = await SeedRoleAsync(db, "Wanted", Permission.ImportMobigo);
        await SeedRoleAsync(db, "Other", Permission.ManageRoles);

        var result = await CreateSut(db).GetAsync(wantedId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(wantedId);
        result.Name.Should().Be("Wanted");
        result.Permissions.Should().Equal(Permission.ImportMobigo);
    }

    [Fact]
    public async Task GetAsync_RoleDoesNotExist_ReturnsNull()
    {
        await using var db = Db();
        await SeedRoleAsync(db, "Existing");

        var result = await CreateSut(db).GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    /****************CreateAsync*******************/

    [Fact]
    public async Task CreateAsync_NameIsFree_ReturnsCreatedRole()
    {
        await using var db = Db();
        var request = CreateRequest("Admin", "Administratör", Permission.ManageRoles, Permission.ImportMobigo);

        var result = await CreateSut(db).CreateAsync(request);

        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Admin");
        result.Description.Should().Be("Administratör");
        result.Permissions.Should().Equal(Permission.ImportMobigo, Permission.ManageRoles);
        result.UserCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_SavesRoleAndPermissionsInDatabase()
    {
        await using var db = Db();

        var result = await CreateSut(db).CreateAsync(
            CreateRequest("Admin", null, Permission.ManageRoles, Permission.ImportVisma4010));

        (await GetRolePermissionNamesAsync(db, result!.Id))
            .Should().BeEquivalentTo(["ManageRoles", "ImportVisma4010"]);
    }

    [Fact]
    public async Task CreateAsync_TrimsNameAndDescription()
    {
        await using var db = Db();

        var result = await CreateSut(db).CreateAsync(CreateRequest("  Admin  ", "  Beskrivning  "));

        result!.Name.Should().Be("Admin");
        result.Description.Should().Be("Beskrivning");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_BlankDescription_StoresNull(string? description)
    {
        await using var db = Db();

        var result = await CreateSut(db).CreateAsync(CreateRequest("Admin", description));

        result!.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicatePermissionsInRequest_StoresEachOnce()
    {
        await using var db = Db();

        var result = await CreateSut(db).CreateAsync(
            CreateRequest("Admin", null, Permission.ManageRoles, Permission.ManageRoles));

        (await GetRolePermissionNamesAsync(db, result!.Id)).Should().Equal("ManageRoles");
    }

    [Fact]
    public async Task CreateAsync_NoPermissions_CreatesRoleWithoutPermissions()
    {
        await using var db = Db();

        var result = await CreateSut(db).CreateAsync(CreateRequest("Läsare"));

        result!.Permissions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("  Admin  ")]
    public async Task CreateAsync_NameIsTaken_ReturnsNullAndCreatesNothing(string name)
    {
        await using var db = Db();
        await SeedRoleAsync(db, "Admin");

        var result = await CreateSut(db).CreateAsync(CreateRequest(name));

        result.Should().BeNull();
        (await CountRolesAsync(db)).Should().Be(1);
    }

    /****************UpdateAsync*******************/

    [Fact]
    public async Task UpdateAsync_RoleDoesNotExist_ReturnsNotFound()
    {
        await using var db = Db();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(),
            CreateRequest("Admin", null, Permission.ManageRoles));

        result.Should().Be(RoleOutcome.NotFound);
    }

    [Theory]
    [InlineData("Läsare")]
    [InlineData("läsare")]
    [InlineData("  LÄSARE ")]
    public async Task UpdateAsync_NameBelongsToAnotherRole_ReturnsNameTakenAndChangesNothing(string newName)
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        await SeedRoleAsync(db, "Läsare");

        var result = await CreateSut(db).UpdateAsync(adminId, CreateRequest(newName, null, Permission.ManageRoles));

        result.Should().Be(RoleOutcome.NameTaken);
        (await GetRoleAsync(db, adminId)).Name.Should().Be("Admin");
    }

    [Fact]
    public async Task UpdateAsync_NameOnlyDiffersInCaseFromItself_IsAllowed()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);

        var result = await CreateSut(db).UpdateAsync(adminId, CreateRequest("admin", null, Permission.ManageRoles));

        result.Should().Be(RoleOutcome.Ok);
        (await GetRoleAsync(db, adminId)).Name.Should().Be("admin");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNameDescriptionAndSyncsPermissions()
    {
        await using var db = Db();
        var roleId = await SeedRoleAsync(db, "Gammal", Permission.ImportMobigo, Permission.ManageRoles);

        var result = await CreateSut(db).UpdateAsync(
            roleId,
            CreateRequest("  Ny  ", "  Ny beskrivning  ", Permission.ManageRoles, Permission.ImportVisma4010));

        result.Should().Be(RoleOutcome.Ok);

        var role = await GetRoleAsync(db, roleId);
        role.Name.Should().Be("Ny");
        role.Description.Should().Be("Ny beskrivning");
        role.RolePermissions.Select(rolePermission => rolePermission.Permission)
            .Should().BeEquivalentTo(["ManageRoles", "ImportVisma4010"]);
    }

    [Fact]
    public async Task UpdateAsync_BlankDescription_ClearsIt()
    {
        await using var db = Db();
        var roleId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);

        await CreateSut(db).UpdateAsync(roleId, CreateRequest("Admin", "   ", Permission.ManageRoles));

        (await GetRoleAsync(db, roleId)).Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_KeepingManageRoles_IsAllowedEvenIfNobodyElseCanManageRoles()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).UpdateAsync(adminId, CreateRequest("Admin", null, Permission.ManageRoles));

        result.Should().Be(RoleOutcome.Ok);
    }

    [Fact]
    public async Task UpdateAsync_RemovingManageRolesFromLastManager_ReturnsLastRoleManagerAndChangesNothing()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).UpdateAsync(adminId, CreateRequest("Ändrad", null, Permission.ImportMobigo));

        result.Should().Be(RoleOutcome.LastRoleManager);

        var role = await GetRoleAsync(db, adminId);
        role.Name.Should().Be("Admin");
        role.RolePermissions.Select(rolePermission => rolePermission.Permission).Should().Equal("ManageRoles");
    }

    [Fact]
    public async Task UpdateAsync_AnotherRoleHasManageRolesButNoUsers_StillCountsAsLastRoleManager()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        await SeedRoleAsync(db, "Reserv", Permission.ManageRoles); // nobody has this role
        await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).UpdateAsync(adminId, CreateRequest("Admin", null));

        result.Should().Be(RoleOutcome.LastRoleManager);
    }

    [Fact]
    public async Task UpdateAsync_AnotherUserCanManageRoles_AllowsRemovingManageRoles()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var superId = await SeedRoleAsync(db, "Super", Permission.ManageRoles);
        await SeedUserAsync(db, "Anna", "Andersson", roleIds: [adminId]);
        await SeedUserAsync(db, "Bo", "Berg", roleIds: [superId]);

        var result = await CreateSut(db).UpdateAsync(adminId, CreateRequest("Admin", null, Permission.ImportMobigo));

        result.Should().Be(RoleOutcome.Ok);
        (await GetRolePermissionNamesAsync(db, adminId)).Should().Equal("ImportMobigo");
    }

    /****************DeleteAsync (only the paths that return before ExecuteDeleteAsync)*******************/

    [Fact]
    public async Task DeleteAsync_RoleIsTheOnlyWayToManageRoles_ReturnsLastRoleManagerAndKeepsRole()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).DeleteAsync(adminId);

        result.Should().Be(RoleOutcome.LastRoleManager);
        (await CountRolesAsync(db)).Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_OtherManagerRoleHasNoUsers_ReturnsLastRoleManager()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        await SeedRoleAsync(db, "Reserv", Permission.ManageRoles); // nobody has this role
        await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).DeleteAsync(adminId);

        result.Should().Be(RoleOutcome.LastRoleManager);
    }

    /****************GetUsersAsync*******************/

    [Fact]
    public async Task GetUsersAsync_NoUsers_ReturnsEmptyList()
    {
        await using var db = Db();

        var result = await CreateSut(db).GetUsersAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsersAsync_SortsByFirstNameThenLastName()
    {
        await using var db = Db();
        await SeedUserAsync(db, "Bo", "Berg");
        await SeedUserAsync(db, "Anna", "Zetterberg");
        await SeedUserAsync(db, "Anna", "Andersson");

        var result = await CreateSut(db).GetUsersAsync();

        result.Select(user => $"{user.FirstName} {user.LastName}")
            .Should().Equal("Anna Andersson", "Anna Zetterberg", "Bo Berg");
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsActivationStatusAndRoleIds()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin");
        var readerId = await SeedRoleAsync(db, "Läsare");
        await SeedUserAsync(db, "Anna", "Andersson", activated: true, roleIds: [adminId, readerId]);
        await SeedUserAsync(db, "Bo", "Berg", activated: false);

        var result = await CreateSut(db).GetUsersAsync();

        var anna = result.Single(user => user.FirstName == "Anna");
        anna.IsActivated.Should().BeTrue();
        anna.RoleIds.Should().BeEquivalentTo([adminId, readerId]);

        var bo = result.Single(user => user.FirstName == "Bo");
        bo.IsActivated.Should().BeFalse();
        bo.RoleIds.Should().BeEmpty();
    }

    /****************SetUserRolesAsync*******************/

    [Fact]
    public async Task SetUserRolesAsync_UserDoesNotExist_ReturnsNotFound()
    {
        await using var db = Db();
        var roleId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);

        var result = await CreateSut(db).SetUserRolesAsync(Guid.NewGuid(), CreateRolesRequest(roleId));

        result.Should().Be(RoleOutcome.NotFound);
    }

    [Fact]
    public async Task SetUserRolesAsync_AnyRoleDoesNotExist_ReturnsUnknownRoleAndChangesNothing()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var userId = await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).SetUserRolesAsync(userId, CreateRolesRequest(adminId, Guid.NewGuid()));

        result.Should().Be(RoleOutcome.UnknownRole);
        (await GetUserRoleIdsAsync(db, userId)).Should().Equal(adminId);
    }

    [Fact]
    public async Task SetUserRolesAsync_ReplacesTheUsersRoles()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var readerId = await SeedRoleAsync(db, "Läsare");
        var importerId = await SeedRoleAsync(db, "Importör", Permission.ImportMobigo);
        var userId = await SeedUserAsync(db, roleIds: [adminId, readerId]);

        var result = await CreateSut(db).SetUserRolesAsync(userId, CreateRolesRequest(adminId, importerId));

        result.Should().Be(RoleOutcome.Ok);
        (await GetUserRoleIdsAsync(db, userId)).Should().BeEquivalentTo([adminId, importerId]);
    }

    [Fact]
    public async Task SetUserRolesAsync_DuplicateRoleIds_AssignsTheRoleOnce()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var userId = await SeedUserAsync(db);

        var result = await CreateSut(db).SetUserRolesAsync(userId, CreateRolesRequest(adminId, adminId));

        result.Should().Be(RoleOutcome.Ok);
        (await GetUserRoleIdsAsync(db, userId)).Should().Equal(adminId);
    }

    [Fact]
    public async Task SetUserRolesAsync_NewRolesKeepManageRoles_IsAllowedEvenForTheOnlyManager()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var readerId = await SeedRoleAsync(db, "Läsare");
        var userId = await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).SetUserRolesAsync(userId, CreateRolesRequest(adminId, readerId));

        result.Should().Be(RoleOutcome.Ok);
    }

    [Fact]
    public async Task SetUserRolesAsync_RemovingManageRolesFromTheOnlyManager_ReturnsLastRoleManagerAndChangesNothing()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var readerId = await SeedRoleAsync(db, "Läsare");
        var userId = await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).SetUserRolesAsync(userId, CreateRolesRequest(readerId));

        result.Should().Be(RoleOutcome.LastRoleManager);
        (await GetUserRoleIdsAsync(db, userId)).Should().Equal(adminId);
    }

    [Fact]
    public async Task SetUserRolesAsync_ClearingRolesOfTheOnlyManager_ReturnsLastRoleManager()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var userId = await SeedUserAsync(db, roleIds: [adminId]);

        var result = await CreateSut(db).SetUserRolesAsync(userId, CreateRolesRequest());

        result.Should().Be(RoleOutcome.LastRoleManager);
        (await GetUserRoleIdsAsync(db, userId)).Should().Equal(adminId);
    }

    [Fact]
    public async Task SetUserRolesAsync_AnotherUserCanManageRoles_AllowsRemovingManageRoles()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var readerId = await SeedRoleAsync(db, "Läsare");
        var annaId = await SeedUserAsync(db, "Anna", "Andersson", roleIds: [adminId]);
        await SeedUserAsync(db, "Bo", "Berg", roleIds: [adminId]);

        var result = await CreateSut(db).SetUserRolesAsync(annaId, CreateRolesRequest(readerId));

        result.Should().Be(RoleOutcome.Ok);
        (await GetUserRoleIdsAsync(db, annaId)).Should().Equal(readerId);
    }

    [Fact]
    public async Task SetUserRolesAsync_ClearingRolesWhileAnotherUserCanManageRoles_RemovesAllRoles()
    {
        await using var db = Db();
        var adminId = await SeedRoleAsync(db, "Admin", Permission.ManageRoles);
        var annaId = await SeedUserAsync(db, "Anna", "Andersson", roleIds: [adminId]);
        await SeedUserAsync(db, "Bo", "Berg", roleIds: [adminId]);

        var result = await CreateSut(db).SetUserRolesAsync(annaId, CreateRolesRequest());

        result.Should().Be(RoleOutcome.Ok);
        (await GetUserRoleIdsAsync(db, annaId)).Should().BeEmpty();
    }
}