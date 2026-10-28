using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.API.Dtos;

public class RegisterRequestDto
{

    [Required, StringLength(100)]
    public string FirstName {get; set;} = string.Empty;

    [Required, StringLength(100)]
    public string LastName {get; set;} = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email {get; set;} = string.Empty;

    [Required, MinLength(8), StringLength(100)]
    public string Password {get; set;} = string.Empty;

    
}