namespace SmartStashAI.Api.Models;

public class ApplicationUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Każdy użytkownik musi należeć do jakiegoś gospodarstwa domowego
    public int HouseholdId { get; set; }
    public Household Household { get; set; } = null!;
}