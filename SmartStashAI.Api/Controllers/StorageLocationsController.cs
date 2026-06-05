using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SmartStashAI.Api.Data;
using SmartStashAI.Api.Models;
using SmartStashAI.Shared.Dtos;
using System.Security.Claims;

namespace SmartStashAI.Api.Controllers;

[Authorize] // Kluczowe: Każdy endpoint w tym kontrolerze wymaga nagłówka Authorization: Bearer <token>
[ApiController]
[Route("api/[controller]")]
public class StorageLocationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StorageLocationsController(AppDbContext context)
    {
        _context = context;
    }

    // Pomocnicza metoda pobierająca ID domu z tokenu zalogowanego użytkownika
    private int GetUserHouseholdId()
    {
        var claim = User.FindFirst("HouseholdId");
        if (claim == null) throw new UnauthorizedAccessException("Brak HouseholdId w tokenie.");
        return int.Parse(claim.Value);
    }

    [HttpPost]
    public async Task<ActionResult<StorageLocationResponseDto>> CreateLocation(CreateLocationDto dto)
    {
        // 1. Pobierz ID gospodarstwa domowego zalogowanego użytkownika
        int householdId = GetUserHouseholdId();

        var location = new StorageLocation
        {
            Name = dto.Name,
            ParentLocationId = dto.ParentLocationId == 0 ? null : dto.ParentLocationId,
            QrCodeToken = "STASH_" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12).ToUpper(),
            HouseholdId = householdId // <-- KLUCZOWA POPRAWKA: Przypisanie do aktualnego domu
        };

        _context.StorageLocations.Add(location);
        await _context.SaveChangesAsync();

        // Mapowanie na obiekt odpowiedzi
        var responseDto = new StorageLocationResponseDto
        {
            Id = location.Id,
            Name = location.Name,
            QrCodeToken = location.QrCodeToken,
            ParentLocationId = location.ParentLocationId,
            Items = new List<ItemResponseDto>(),
            ChildLocations = new List<StorageLocationResponseDto>()
        };

        return CreatedAtAction(nameof(CreateLocation), new { id = location.Id }, responseDto);
    }

    // Wyszukiwanie lokalizacji i jej zawartości na podstawie zeskanowanego kodu QR (Opcja Wyszukiwania 1)
    [HttpGet("qr/{qrToken}")]
    public async Task<ActionResult<StorageLocationResponseDto>> GetLocationByQr(string qrToken)
    {
        int householdId = GetUserHouseholdId();

        // Pobieramy lokalizację wraz z przedmiotami oraz bezpośrednimi podlokalizacjami (szufladami)
        var location = await _context.StorageLocations
            .Include(l => l.Items)
            .Include(l => l.ChildLocations)
                .ThenInclude(cl => cl.Items) // Dociągamy też rzeczy z szuflad wewnątrz szafy
            .FirstOrDefaultAsync(l => l.QrCodeToken == qrToken && l.HouseholdId == householdId);

        if (location == null)
        {
            return NotFound("Nie znaleziono schowka przypisanego do tego kodu QR w Twoim gospodarstwie domowym.");
        }

        // Mapowanie na DTO
        var response = new StorageLocationResponseDto
        {
            Id = location.Id,
            Name = location.Name,
            QrCodeToken = location.QrCodeToken,
            ParentLocationId = location.ParentLocationId,
            Items = location.Items.Select(i => new ItemResponseDto
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Purpose = i.Purpose,
                IsLost = i.IsLost
            }).ToList(),
            ChildLocations = location.ChildLocations.Select(cl => new StorageLocationResponseDto
            {
                Id = cl.Id,
                Name = cl.Name,
                QrCodeToken = cl.QrCodeToken,
                ParentLocationId = cl.ParentLocationId,
                Items = cl.Items.Select(ci => new ItemResponseDto
                {
                    Id = ci.Id,
                    Name = ci.Name,
                    Category = ci.Category,
                    Purpose = ci.Purpose,
                    IsLost = ci.IsLost
                }).ToList()
            }).ToList()
        };

        return Ok(response);
    }

    // Endpoint generujący fizyczny obrazek kodu QR w formacie PNG
    [HttpGet("{id}/qr-image")]
    [AllowAnonymous] // Pozwalamy na bezpośrednie osadzenie w tagu <img src="..."> na froncie
    public async Task<IActionResult> GetQrImage(int id)
    {
        var location = await _context.StorageLocations.FirstOrDefaultAsync(l => l.Id == id);
        if (location == null) return NotFound();

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(location.QrCodeToken, QRCodeGenerator.ECCLevel.Q);
        using var ppngByteQrCode = new PngByteQRCode(qrCodeData);

        byte[] qrCodeAsPngByteArr = ppngByteQrCode.GetGraphic(20); // Liczba określa wielkość/piksele na moduł

        return File(qrCodeAsPngByteArr, "image/png");
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<StorageLocationResponseDto>>> GetAllRootLocations()
    {
        int householdId = GetUserHouseholdId();

        // Pobieramy szafy najwyższego poziomu (ParentLocationId == null) 
        // i JAWNIE dołączamy podlokalizacje (ChildLocations) oraz przedmioty (Items)
        var rootLocations = await _context.StorageLocations
            .Include(l => l.Items)
            .Include(l => l.ChildLocations)
                .ThenInclude(cl => cl.Items)
            .Include(l => l.ChildLocations)
                .ThenInclude(cl => cl.ChildLocations) // Wsparcie dla 3 poziomu (np. Szafa -> Szuflada -> Organizer)
            .Where(l => l.ParentLocationId == null && l.HouseholdId == householdId)
            .ToListAsync();

        var response = rootLocations.Select(l => MapToDto(l)).ToList();
        return Ok(response);
    }

    private static StorageLocationResponseDto MapToDto(StorageLocation loc)
    {
        return new StorageLocationResponseDto
        {
            Id = loc.Id,
            Name = loc.Name,
            QrCodeToken = loc.QrCodeToken,
            ParentLocationId = loc.ParentLocationId,
            Items = loc.Items?.Select(i => new ItemResponseDto
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Purpose = i.Purpose,
                IsLost = i.IsLost
            }).ToList() ?? new List<ItemResponseDto>(),
            ChildLocations = loc.ChildLocations?.Select(cl => MapToDto(cl)).ToList() ?? new List<StorageLocationResponseDto>()
        };
    }
}