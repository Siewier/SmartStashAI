namespace SmartStashAI.Shared.Dtos;

public class CreateItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int StorageLocationId { get; set; }
}

public class UpdateItemStatusDto
{
    public bool IsLost { get; set; }
}

public class FullItemResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public bool IsLost { get; set; }
    public int StorageLocationId { get; set; }
    public string StorageLocationName { get; set; } = string.Empty;
    public string ParentLocationsPath { get; set; } = string.Empty; // Np. "Piwnica -> Regał A -> Pudełko 2"
}