namespace SmartStashAI.Api.Dtos;

public class CreateLocationDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentLocationId { get; set; } // Opcjonalnie: ID szafki nadrzędnej
}

public class StorageLocationResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QrCodeToken { get; set; } = string.Empty;
    public int? ParentLocationId { get; set; }
    public List<StorageLocationResponseDto> ChildLocations { get; set; } = new();
    public List<ItemResponseDto> Items { get; set; } = new();
}

public class ItemResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public bool IsLost { get; set; }
}