namespace SmartStashAI.Api.Services;

public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    string GenerateJwtToken(int userId, string username, int householdId);
}