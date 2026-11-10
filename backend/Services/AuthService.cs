using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Authorization;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Models;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SeconSbtiBackend.Services;

public class AuthService(ApplicationDbContext db) : IAuthService
{
    public async Task<AppUser?> RegisterAsync(RegisterRequestDTO request)
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
            .FirstOrDefaultAsync(u => u.Email == normalized);

        if (user?.PasswordHash is null)
            return null;

        return BCryptNet.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<AppUser?> LoginAsync(LoginRequestDTO request)
    {
        return await ValidateCredentialsAsync(request.Email, request.Password);
    }

    public async Task<CurrentUserDTO?> GetCurrentUserAsync(Guid userId)
    {
        var user = await db.AppUsers
            .Include(u => u.Roles)
                .ThenInclude(r => r.RolePermissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return null;

        var permissions = new List<Permission>();

        foreach (var rolePermission in user.Roles.SelectMany(r => r.RolePermissions))
        {
            // Skips names that aren't in the Permission enum
            if (Enum.TryParse(rolePermission.Permission, out Permission permission)
                && Enum.IsDefined(permission)
                && !permissions.Contains(permission))
            {
                permissions.Add(permission);
            }
        }

        return new CurrentUserDTO
        {
            Id = user.Id,
            Name = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            Roles = user.Roles.Select(r => r.Name).Order().ToList(),
            Permissions = permissions
        };
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}