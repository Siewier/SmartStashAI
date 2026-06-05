namespace SmartStashAI.Api.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? ImagePath { get; set; } // Ścieżka do zapisanego lokalnie zdjęcia
    public bool IsLost { get; set; } = false; // Flaga dla przedmiotów zgubionych

    // Przedmiot fizycznie leży w jakiejś szafce/pudełku
    public int StorageLocationId { get; set; }
    public StorageLocation StorageLocation { get; set; } = null!;
}