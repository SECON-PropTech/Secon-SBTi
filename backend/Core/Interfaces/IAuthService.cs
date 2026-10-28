using SeconSbtiBackend.API.Dtos;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Core.Interfaces;

public interface IAuthService
{
    Task<AppUser?> RegisterAsync(RegisterRequestDto request);
    Task<AppUser?> ValidateCredentialsAsync(string email, string password);
}