using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartStashAI.Api.Services;

public class AuthService : IAuthService
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly IConfiguration _configuration;

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string HashPassword(string password) =>
        _hasher.HashPassword(new object(), password);

    public bool VerifyPassword(string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(new object(), passwordHash, password);
        return result != PasswordVerificationResult.Failed;
    }

    public string GenerateJwtToken(int userId, string username, int householdId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["Jwt:Secret"] ?? "SuperTajnyKluczDoSmartStashAI2026!TrzymajGoWBezpiecznymMiejscu";
        var key = Encoding.ASCII.GetBytes(secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim("HouseholdId", householdId.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(30),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}