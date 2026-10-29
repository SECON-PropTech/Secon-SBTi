namespace SeconSbtiBackend.DTOs;

public class UserDTO
{
    public Guid Id {get; set;}
    public string FirstName {get; set;} = string.Empty;
    public string LastName {get; set;} = string.Empty;
    public string? Email {get; set;}

}