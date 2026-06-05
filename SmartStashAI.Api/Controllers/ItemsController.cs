using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStashAI.Api.Data;
using SmartStashAI.Api.Models;
using SmartStashAI.Shared.Dtos;
using System.Security.Claims;

namespace SmartStashAI.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ItemsController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserHouseholdId()
    {
        var claim = User.FindFirst("HouseholdId");
        if (claim == null) throw new UnauthorizedAccessException("Brak HouseholdId w tokenie.");
        return int.Parse(claim.Value);
    }

    // 1. DODAWANIE PRZEDMIOTU DO SZUFLADY
    [HttpPost]
    public async Task<ActionResult<FullItemResponseDto>> CreateItem([FromBody] CreateItemDto dto)
    {
        int householdId = GetUserHouseholdId();

        // Upewniamy się, że docelowa lokalizacja istnieje i należy do tego samego domu
        var location = await _context.StorageLocations
            .FirstOrDefaultAsync(l => l.Id == dto.StorageLocationId && l.HouseholdId == householdId);

        if (location == null)
        {
            return BadRequest("Wybrana lokalizacja nie istnieje lub nie masz do niej uprawnień.");
        }

        var item = new Item
        {
            Name = dto.Name,
            Category = dto.Category,
            Purpose = dto.Purpose,
            StorageLocationId = dto.StorageLocationId,
            IsLost = false
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        return Ok(new FullItemResponseDto
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            Purpose = item.Purpose,
            IsLost = item.IsLost,
            StorageLocationId = item.StorageLocationId,
            StorageLocationName = location.Name
        });
    }

    // 2. ZMIANA STATUSU (Oznaczanie jako zgubiony / znaleziony)
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateItemStatus(int id, [FromBody] UpdateItemStatusDto dto)
    {
        int householdId = GetUserHouseholdId();

        // Szukamy przedmiotu dbając o to, by należał do szafki powiązanej z domem użytkownika
        var item = await _context.Items
            .Include(i => i.StorageLocation)
            .FirstOrDefaultAsync(i => i.Id == id && i.StorageLocation.HouseholdId == householdId);

        if (item == null)
        {
            return NotFound("Nie znaleziono przedmiotu o podanym ID w Twoim gospodarstwie domowym.");
        }

        item.IsLost = dto.IsLost;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // 3. WYSZUKIWANIE PRZEDMIOTU (Wyszukiwanie opcja druga)
    [HttpGet("search")]
    public async Task<ActionResult<List<FullItemResponseDto>>> SearchItems([FromQuery] string query)
    {
        int householdId = GetUserHouseholdId();

        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Zapytanie wyszukiwania nie może być puste.");
        }

        // Pobieramy przedmioty pasujące do frazy z uwzględnieniem HouseholdId
        var items = await _context.Items
            .Include(i => i.StorageLocation)
            .Where(i => i.StorageLocation.HouseholdId == householdId &&
                        (i.Name.ToLower().Contains(query.ToLower()) ||
                         i.Category.ToLower().Contains(query.ToLower()) ||
                         i.Purpose.ToLower().Contains(query.ToLower())))
            .ToListAsync();

        var response = new List<FullItemResponseDto>();

        foreach (var item in items)
        {
            // Budujemy pełną ścieżkę lokalizacji (np. "Piwnica -> Regał A -> Szuflada 1")
            var pathParts = new List<string> { item.StorageLocation.Name };
            var currentParentId = item.StorageLocation.ParentLocationId;

            while (currentParentId != null)
            {
                var parent = await _context.StorageLocations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == currentParentId);
                if (parent != null)
                {
                    pathParts.Insert(0, parent.Name);
                    currentParentId = parent.ParentLocationId;
                }
                else
                {
                    currentParentId = null;
                }
            }

            response.Add(new FullItemResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category,
                Purpose = item.Purpose,
                IsLost = item.IsLost,
                StorageLocationId = item.StorageLocationId,
                StorageLocationName = item.StorageLocation.Name,
                ParentLocationsPath = string.Join(" -> ", pathParts)
            });
        }

        return Ok(response);
    }
}