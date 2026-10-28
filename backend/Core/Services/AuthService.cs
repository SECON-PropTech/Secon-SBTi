using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.API.Dtos;
using SeconSbtiBackend.Core.Interfaces;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Models;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SeconSbtiBackend.Core.Services;

public class AuthService(ApplicationDbContext db) : IAuthService
{
    public async Task<AppUser?> RegisterAsync(RegisterRequestDto request)
    {
        var email = Normalize(request.Email);

        if (await db.AppUsers.AnyAsync(u => u.Email == email))
            return null;

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = BCryptNet.HashPassword(request.Password)
        };

        db.AppUsers.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return null;
        }

        return user;
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string email, string password)
    {
        var normalized = Normalize(email);

        var user = await db.AppUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == normalized);

        if (user?.PasswordHash is null)
            return null;

        return BCryptNet.Verify(password, user.PasswordHash) ? user : null;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}