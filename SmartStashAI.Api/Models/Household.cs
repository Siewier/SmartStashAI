using System.Security.Cryptography.X509Certificates;

namespace SmartStashAI.Api.Models;

public class Household
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Relacje
    public List<ApplicationUser> Members { get; set; } = new();
    public List<StorageLocation> StorageLocations { get; set; } = new();
}