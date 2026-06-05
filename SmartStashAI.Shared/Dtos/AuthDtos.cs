namespace SmartStashAI.Shared.Dtos;
public class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string HouseholdName { get; set; } = string.Empty; // Nazwa nowego domu, np. "Dom Kowalskich"
}

public class JoinHouseholdRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int HouseholdIdToJoin { get; set; } // ID istniejącego domu, do którego dołączamy
}

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int HouseholdId { get; set; }
}