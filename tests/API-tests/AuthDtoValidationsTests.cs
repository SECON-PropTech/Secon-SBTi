using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SeconSbtiBackend.DTOs;
 
namespace api_tests;
 
public class AuthDtoValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
 
    private static RegisterRequestDTO ValidRegister() => new()
    {
        FirstName = "Anna",
        LastName = "Testsson",
        Email = "anna@secon.se",
        Password = "password111"
    };

    /******************RegisterRequestDTO*******************/
    [Fact]
    public void Register_ValidRequest_HasNoErrors()
    {
        Validate(ValidRegister()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(nameof(RegisterRequestDTO.FirstName))]
    [InlineData(nameof(RegisterRequestDTO.LastName))]
    [InlineData(nameof(RegisterRequestDTO.Email))]
    [InlineData(nameof(RegisterRequestDTO.Password))]
    public void Register_EmptyRequiredField_IsInvalid(string property)
    {
        var dto = ValidRegister();
        typeof(RegisterRequestDTO).GetProperty(property)!.SetValue(dto, string.Empty);
        Validate(dto).SelectMany(r => r.MemberNames).Should().Contain(property);
    }

    [Theory]
    [InlineData("inte-en-epost")]
    [InlineData("saknar@")]
    [InlineData("@saknar.se")]
    public void Register_InvalidEmail_IsInvalid(string email)
    {
        var dto = ValidRegister();
        dto.Email = email;

        Validate(dto).SelectMany(r => r.MemberNames).Should().Contain(nameof(RegisterRequestDTO.Email));
    }
 
    [Fact]
    public void Register_PasswordShorterThan8_IsInvalid()
    {
        var dto = ValidRegister();
        dto.Password = "short";
        Validate(dto).SelectMany(r => r.MemberNames).Should().Contain(nameof(RegisterRequestDTO.Password));
    }
 
    [Fact]
    public void Register_PasswordExactly8_IsValid()
    {
        var dto = ValidRegister();
        dto.Password = "12345678";
        Validate(dto).Should().BeEmpty();
    }
 
    [Fact]
    public void Register_PasswordLongerThan100_IsInvalid()
    {
        var dto = ValidRegister();
        dto.Password = new string('a', 101);
        Validate(dto).SelectMany(r => r.MemberNames).Should().Contain(nameof(RegisterRequestDTO.Password));
    }
 
    [Fact]
    public void Register_NamesLongerThan100_AreInvalid()
    {
        var dto = ValidRegister();
        dto.FirstName = new string('a', 101);
        dto.LastName = new string('b', 101);
        Validate(dto).SelectMany(r => r.MemberNames).Should()
            .Contain(new[] { nameof(RegisterRequestDTO.FirstName), nameof(RegisterRequestDTO.LastName) });
    }
 
    [Fact]
    public void Register_EmailLongerThan254_IsInvalid()
    {
        var dto = ValidRegister();
        dto.Email = new string('a', 250) + "@b.se";
        Validate(dto).SelectMany(r => r.MemberNames).Should().Contain(nameof(RegisterRequestDTO.Email));
    }
 
    /*********************LoginRequestDTO**********************/
 
    [Fact]
    public void Login_ValidRequest_HasNoErrors()
    {
        var dto = new LoginRequestDTO { Email = "anna@secon.se", Password = "password123" };
        Validate(dto).Should().BeEmpty();
    }
 
    [Fact]
    public void Login_MissingFields_AreInvalid()
    {
        var dto = new LoginRequestDTO();
        Validate(dto).SelectMany(r => r.MemberNames).Should()
            .Contain(new[] { nameof(LoginRequestDTO.Email), nameof(LoginRequestDTO.Password) });
    }
 
    [Fact]
    public void Login_InvalidEmail_IsInvalid()
    {
        var dto = new LoginRequestDTO { Email = "not-email", Password = "x" };
        Validate(dto).SelectMany(r => r.MemberNames).Should().Contain(nameof(LoginRequestDTO.Email));
    }
}