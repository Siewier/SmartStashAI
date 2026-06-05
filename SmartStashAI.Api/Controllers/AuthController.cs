using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStashAI.Api.Data;
using SmartStashAI.Api.Models;
using SmartStashAI.Api.Services;
using SmartStashAI.Shared.Dtos;

namespace SmartStashAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;

    public AuthController(AppDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
        {
            return BadRequest("Użytkownik o takiej nazwie już istnieje.");
        }

        // 1. Tworzymy nowe gospodarstwo domowe dla pierwszego użytkownika
        var household = new Household
        {
            Name = string.IsNullOrWhiteSpace(request.HouseholdName) ? $"Dom {request.Username}" : request.HouseholdName
        };
        _context.Households.Add(household);
        await _context.SaveChangesAsync(); // Zapisujemy, aby wygenerować Household.Id

        // 2. Tworzymy użytkownika i przypisujemy go do nowo stworzonego domu
        var user = new ApplicationUser
        {
            Username = request.Username,
            PasswordHash = _authService.HashPassword(request.Password),
            HouseholdId = household.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 3. Generujemy token JWT
        var token = _authService.GenerateJwtToken(user.Id, user.Username, user.HouseholdId);

        return Ok(new AuthResponseDto { Token = token, Username = user.Username, HouseholdId = user.HouseholdId });
    }

    [HttpPost("join")]
    public async Task<ActionResult<AuthResponseDto>> JoinHousehold([FromBody] JoinHouseholdRequestDto request)
    {
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
        {
            return BadRequest("Użytkownik o takiej nazwie już istnieje.");
        }

        var householdExists = await _context.Households.AnyAsync(h => h.Id == request.HouseholdIdToJoin);
        if (!householdExists)
        {
            return NotFound("Nie znaleziono gospodarstwa domowego o podanym ID.");
        }

        // Tworzymy użytkownika i przypisujemy go do ISTNIEJĄCEGO domu (rodziny)
        var user = new ApplicationUser
        {
            Username = request.Username,
            PasswordHash = _authService.HashPassword(request.Password),
            HouseholdId = request.HouseholdIdToJoin
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _authService.GenerateJwtToken(user.Id, user.Username, user.HouseholdId);
        return Ok(new AuthResponseDto { Token = token, Username = user.Username, HouseholdId = user.HouseholdId });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());
        if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized("Nieprawidłowy login lub hasło.");
        }

        var token = _authService.GenerateJwtToken(user.Id, user.Username, user.HouseholdId);
        return Ok(new AuthResponseDto { Token = token, Username = user.Username, HouseholdId = user.HouseholdId });
    }
}