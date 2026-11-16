using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;
using BCryptNet = BCrypt.Net.BCrypt;

namespace api_tests;

public class AuthServiceTests
{
    private const string Password = "password111";

    private static DbContextOptions<ApplicationDbContext> NewOptions() => 
        new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static ApplicationDbContext Db() => new ApplicationDbContext(NewOptions());
    private static AuthService CreateSut(ApplicationDbContext db) => new(db);


    private static async Task SeedUserAsync(
        ApplicationDbContext db,
        string email = "anna@secon.se",
        string? password = Password
    )
    {
       db.AppUsers.Add(new AppUser
       {
           FirstName = "Anna",
            LastName = "Testsson",
            Email = email,
            PasswordHash = password == null ? null : BCryptNet.HashPassword(password)
        });

       await db.SaveChangesAsync();
    }

    private static RegisterRequestDTO RegisterRequest(string email = "anna@secon.se") => new ()
    {
        FirstName = "Anna",
        LastName = "Testsson",
        Email = email,
        Password = Password
    };

    private static LoginRequestDTO LoginRequest(
        string email = "anna@secon.se",
        string password = Password) => new()
    {
        Email = email,
        Password = password
    };

    private sealed class ThrowingDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
            => throw new DbUpdateException("Error silumation");
    }

    /****************RegisterAsync*******************/

    [Fact]
    public async Task RegisterAsync_ReturnsUserAndPersistsIt()
    {
        await using var db = Db();
        var sut = CreateSut(db);
        var result = await sut.RegisterAsync(RegisterRequest());
        result.Should().NotBeNull();
        result!.Email.Should().Be("anna@secon.se");
        (await db.AppUsers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailAndTrimsNames()
    {
        await using var db = Db();
        var sut = CreateSut(db);

        var result = await sut.RegisterAsync(new RegisterRequestDTO
        {
            FirstName = "Anna",
            LastName = "Testsson",
            Email = "anna@secon.se",
            Password = Password
        });

        result.Should().NotBeNull();
        result!.Email.Should().Be("anna@secon.se");
        result.FirstName.Should().Be("Anna");
        result.LastName.Should().Be("Testsson");
    }

    [Fact]
    public async Task RegisterAsync_HashedPassword_NeverStoresPlainText()
    {
        await using var db = Db();
        var sut = CreateSut(db);
        var result = await sut.RegisterAsync(RegisterRequest());
        result!.PasswordHash.Should().NotBe(Password);
        BCryptNet.Verify(Password, result.PasswordHash).Should().BeTrue(); 
    }

    [Fact]
    public async Task RegisterAsync_SamePasswordTwice_ProducesDifferentHashes()
    {
        await using var db = Db();
        var sut = CreateSut(db);

        var first = await sut.RegisterAsync(RegisterRequest("a@secon.se"));
        var second = await sut.RegisterAsync(RegisterRequest("b@secon.se"));

        first!.PasswordHash.Should().NotBe(second!.PasswordHash);
    }

    [Theory]
    [InlineData("anna@secon.se")]
    [InlineData("ANNA@SECON.SE")]
    [InlineData("  anna@secon.se  ")]
    public async Task RegisterAsync_DuplicateEmail_ReturnsNullAndDoesNotAddUser(string email)
    {
        await using var db = Db();
        await SeedUserAsync(db);
        var result = await CreateSut(db).RegisterAsync(RegisterRequest(email));
        result.Should().BeNull();
        (await db.AppUsers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_WhenSaveFails_ReturnsNull()
    {
        await using var db = new ThrowingDbContext(NewOptions());
        var result = await CreateSut(db).RegisterAsync(RegisterRequest());
        result.Should().BeNull();
    }

    /*****************ValidateCredentialsAsync*******************/
    [Fact]
    public async Task ValidateCredentialsAsyncc_CorrectCredentials_ReturnsUser()
    {
        await using var db = Db();
        await SeedUserAsync(db);
        var result = await CreateSut(db).ValidateCredentialsAsync("anna@secon.se", Password);
        result.Should().NotBeNull();
        result!.Email.Should().Be("anna@secon.se");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WrongPassword_ReturnsNull()
    {
        await using var db = Db();
        await SeedUserAsync(db);
        var result = await CreateSut(db).ValidateCredentialsAsync("anna@secon.se", "wrongpassword");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_UnknownEmail_ReturnsNull()
    {
        await using var db = Db();
        var result = await CreateSut(db).ValidateCredentialsAsync("unknown@secon.se", Password);
        result.Should().BeNull();
    }

    /******************LoginAsync*********************/
    [Fact]
    public async Task LoginAsync_ValidRequest_ReturnsUser()
    {
        await using var db = Db();
        await SeedUserAsync(db);
        var result = await CreateSut(db).LoginAsync(LoginRequest());
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        await using var db = Db();
        await SeedUserAsync(db);
        var result = await CreateSut(db).LoginAsync(LoginRequest(password: "wrongPassword"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterThenLogin_WorksEndToEnd()
    {
        await using var db = Db();
        var sut = CreateSut(db);
        await sut.RegisterAsync(RegisterRequest());
        var result = await sut.LoginAsync(LoginRequest(email: "anna@secon.se"));
        result.Should().NotBeNull();
    }
}
