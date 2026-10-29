using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Interfaces;

public interface IAuthService
{
    Task<AppUser?> RegisterAsync(RegisterRequestDTO request);
    Task<AppUser?> ValidateCredentialsAsync(string email, string password);
}