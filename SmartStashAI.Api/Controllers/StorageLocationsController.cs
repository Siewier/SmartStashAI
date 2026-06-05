using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SmartStashAI.Api.Data;
using SmartStashAI.Api.Dtos;
using SmartStashAI.Api.Models;
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
    public async Task<ActionResult<StorageLocationResponseDto>> CreateLocation([FromBody] CreateLocationDto dto)
    {
        int householdId = GetUserHouseholdId();

        // Generujemy unikalny token dla kodu QR (np. STASH_GUID)
        string qrToken = $"STASH_{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";

        var location = new StorageLocation
        {
            Name = dto.Name,
            QrCodeToken = qrToken,
            HouseholdId = householdId,
            ParentLocationId = dto.ParentLocationId
        };

        _context.StorageLocations.Add(location);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLocationByQr), new { qrToken = location.QrCodeToken }, new StorageLocationResponseDto
        {
            Id = location.Id,
            Name = location.Name,
            QrCodeToken = location.QrCodeToken,
            ParentLocationId = location.ParentLocationId
        });
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
}