using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StAmanuelBloodDonation.Api.Data;
using StAmanuelBloodDonation.Api.Models;

namespace StAmanuelBloodDonation.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    string GenerateToken(Admin admin);
}

public class AuthService(AppDbContext db, IConfiguration config) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var admin = await db.Admins.FirstOrDefaultAsync(a =>
            a.Username == request.Username && a.IsActive);

        if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            return null;

        var expires = DateTime.UtcNow.AddHours(8);
        var token = GenerateToken(admin);

        return new LoginResponse(token, admin.Username, admin.FullName, admin.Role, expires);
    }

    public string GenerateToken(Admin admin)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, admin.Role)
            ],
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public interface IAuditService
{
    Task LogAsync(Guid? adminId, string action, string entityType, Guid? entityId, string? details, string? ipAddress);
}

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task LogAsync(Guid? adminId, string action, string entityType, Guid? entityId, string? details, string? ipAddress)
    {
        db.AuditLogs.Add(new AuditLog
        {
            AdminId = adminId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress
        });
        await db.SaveChangesAsync();
    }
}

public static class DonorEligibility
{
    private const int MinDaysBetweenDonations = 90;

    public static bool CalculateEligibility(DateOnly? lastDonationDate)
    {
        if (lastDonationDate == null) return true;
        var daysSince = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - lastDonationDate.Value.DayNumber;
        return daysSince >= MinDaysBetweenDonations;
    }

    public static string? GetEligibilityNotes(bool isEligible, DateOnly? lastDonationDate)
    {
        if (isEligible) return null;
        if (lastDonationDate == null) return "Not eligible";
        var nextEligible = lastDonationDate.Value.AddDays(MinDaysBetweenDonations);
        return $"Eligible again on {nextEligible:MM/dd/yyyy}";
    }
}
