using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace tests.ServicesTests;

public class PermissionServiceTests
{
    private static DbContextOptions<ApplicationDbContext> NewOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static ApplicationDbContext Db() => new ApplicationDbContext(NewOptions());
    private static PermissionService CreateSut(ApplicationDbContext db) => new(db);

    private static Role CreateRole(IEnumerable<string> permissionNames)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = $"role-{Guid.NewGuid():N}" };
        foreach (var name in permissionNames)
        {
            role.RolePermissions.Add(new RolePermission { Permission = name });
        }
        return role;
    }

    /// <summary>
    /// Seeds a user with one role per permission set. No sets gives a user without roles.
    /// The change tracker is cleared afterwards so the service has to read from the database.
    /// </summary>
    private static async Task<Guid> SeedUserAsync(ApplicationDbContext db, params Permission[][] roles)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Anna",
            LastName = "Testsson",
            Email = $"{Guid.NewGuid():N}@secon.se",
            PasswordHash = "hash"
        };

        foreach (var permissions in roles)
        {
            user.Roles.Add(CreateRole(permissions.Select(permission => permission.ToString())));
        }

        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return user.Id;
    }

    private static async Task AddRoleWithRawPermissionToUserAsync(
        ApplicationDbContext db,
        Guid userId,
        string permissionName)
    {
        var user = await db.AppUsers.Include(u => u.Roles).SingleAsync(u => u.Id == userId);
        var role = CreateRole([permissionName]);
        db.Roles.Add(role);
        user.Roles.Add(role);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static async Task RemoveAllRolesFromUserAsync(ApplicationDbContext db, Guid userId)
    {
        var user = await db.AppUsers.Include(u => u.Roles).SingleAsync(u => u.Id == userId);
        user.Roles.Clear();
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    /****************HasPermissionAsync: Granted*******************/
    [Theory]
    [InlineData(Permission.ImportVisma4010)]
    [InlineData(Permission.ImportMobigo)]
    [InlineData(Permission.ManageRoles)]
    public async Task HasPermissionAsync_UserHasRoleWithPermission_ReturnsTrue(Permission permission)
    {
        await using var db = Db();
        var userId = await SeedUserAsync(db, [permission]);

        var result = await CreateSut(db).HasPermissionAsync(userId, permission);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_OnlyOneOfSeveralRolesHasPermission_ReturnsTrue()
    {
        await using var db = Db();
        var userId = await SeedUserAsync(
            db,
            [Permission.ImportMobigo],
            [Permission.ImportVisma4010],
            [Permission.ManageRoles]);

        var result = await CreateSut(db).HasPermissionAsync(userId, Permission.ManageRoles);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_RoleHasSeveralPermissions_ReturnsTrueOnlyForTheGivenOnes()
    {
        await using var db = Db();
        var userId = await SeedUserAsync(db, [Permission.ImportMobigo, Permission.ManageRoles]);
        var sut = CreateSut(db);

        (await sut.HasPermissionAsync(userId, Permission.ImportMobigo)).Should().BeTrue();
        (await sut.HasPermissionAsync(userId, Permission.ManageRoles)).Should().BeTrue();
        (await sut.HasPermissionAsync(userId, Permission.ImportVisma4010)).Should().BeFalse();
    }

    /****************HasPermissionAsync: Denied*******************/
    [Theory]
    [InlineData(Permission.ImportVisma4010)]
    [InlineData(Permission.ImportMobigo)]
    [InlineData(Permission.ManageRoles)]
    public async Task HasPermissionAsync_UserHasNoRoles_ReturnsFalse(Permission permission)
    {
        await using var db = Db();
        var userId = await SeedUserAsync(db);

        var result = await CreateSut(db).HasPermissionAsync(userId, permission);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_RoleHasNoPermissions_ReturnsFalse()
    {
        await using var db = Db();
        var userId = await SeedUserAsync(db, Array.Empty<Permission>());

        var result = await CreateSut(db).HasPermissionAsync(userId, Permission.ManageRoles);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_RolesOnlyHaveOtherPermissions_ReturnsFalse()
    {
        await using var db = Db();
        var userId = await SeedUserAsync(db, [Permission.ImportMobigo], [Permission.ImportVisma4010]);

        var result = await CreateSut(db).HasPermissionAsync(userId, Permission.ManageRoles);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_AnotherUserHasPermission_ReturnsFalseForThisUser()
    {
        await using var db = Db();
        await SeedUserAsync(db, [Permission.ManageRoles]);
        var otherUserId = await SeedUserAsync(db, [Permission.ImportMobigo]);

        var result = await CreateSut(db).HasPermissionAsync(otherUserId, Permission.ManageRoles);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_UnknownUser_ReturnsFalse()
    {
        await using var db = Db();
        await SeedUserAsync(db, [Permission.ManageRoles]);

        var result = await CreateSut(db).HasPermissionAsync(Guid.NewGuid(), Permission.ManageRoles);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_EmptyGuid_ReturnsFalse()
    {
        await using var db = Db();
        await SeedUserAsync(db, [Permission.ManageRoles]);

        var result = await CreateSut(db).HasPermissionAsync(Guid.Empty, Permission.ManageRoles);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_StoredPermissionNameNotInEnum_DoesNotGrantAnything()
    {
        await using var db = Db();
        var userId = await SeedUserAsync(db);
        await AddRoleWithRawPermissionToUserAsync(db, userId, "SomethingThatDoesNotExist");
        var sut = CreateSut(db);

        (await sut.HasPermissionAsync(userId, Permission.ManageRoles)).Should().BeFalse();
        (await sut.HasPermissionAsync(userId, Permission.ImportMobigo)).Should().BeFalse();
    }

    /****************HasPermissionAsync: always asks the database*******************/
    [Fact]
    public async Task HasPermissionAsync_PermissionGrantedAfterEarlierCall_IsPickedUp()
    {
        await using var db = Db();
        var userId = await SeedUserAsync(db, [Permission.ImportMobigo]);
        var sut = CreateSut(db);
        var before = await sut.HasPermissionAsync(userId, Permission.ManageRoles);
        await AddRoleWithRawPermissionToUserAsync(db, userId, nameof(Permission.ManageRoles));
        var after = await sut.HasPermissionAsync(userId, Permission.ManageRoles);

        before.Should().BeFalse();
        after.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_RoleRemovedAfterEarlierCall_IsPickedUp()
    {
        await using var db = Db();
        var userId = await SeedUserAsync(db, [Permission.ManageRoles]);
        var sut = CreateSut(db);
        var before = await sut.HasPermissionAsync(userId, Permission.ManageRoles);
        await RemoveAllRolesFromUserAsync(db, userId);
        var after = await sut.HasPermissionAsync(userId, Permission.ManageRoles);

        before.Should().BeTrue();
        after.Should().BeFalse();
    }
}