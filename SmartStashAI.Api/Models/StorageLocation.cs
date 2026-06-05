namespace SmartStashAI.Api.Models;

public class StorageLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // np. "Garaż", "Szuflada na kable"
    public string QrCodeToken { get; set; } = string.Empty; // Unikalny identyfikator z naklejki QR

    // Przypisanie do konkretnego domu
    public int HouseholdId { get; set; }
    public Household Household { get; set; } = null!;

    // Rekurencyjna relacja drzewiasta (Rodzic -> Dzieci)
    public int? ParentLocationId { get; set; }
    public StorageLocation? ParentLocation { get; set; }
    public List<StorageLocation> ChildLocations { get; set; } = new();

    // Przedmioty w danej lokalizacji
    public List<Item> Items { get; set; } = new();
}