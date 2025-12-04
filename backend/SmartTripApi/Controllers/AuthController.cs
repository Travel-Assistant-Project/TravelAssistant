using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.Services;
using SmartTripApi.DTOs;
using SmartTripApi.Helpers;

namespace SmartTripApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthController(AppDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new { message = "Email already registered" });

        var user = new User
        {
            Name = dto.Name ?? string.Empty,
            Email = dto.Email ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? string.Empty),
            Age = dto.Age ?? 0,
            Country = dto.Country ?? string.Empty,
            City = dto.City ?? string.Empty,
            CreatedAt = DateTimeHelper.GetTurkeyTime()
        };

        _db.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwt.CreateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            Name = user.Name,
            Email = user.Email
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var token = _jwt.CreateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            Name = user.Name,
            Email = user.Email
        });
    }
}
