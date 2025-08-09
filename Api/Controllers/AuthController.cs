using Application.Abstractions.Auth;
using Application.Features.Auth;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthController(AppDbContext db, IPasswordHasher hasher, ITokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { message = "Email already exists" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = req.Email,
            PasswordHash = _hasher.Hash(req.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        var (refreshToken, refreshExp) = _tokens.CreateRefreshToken();
        var rtEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _hasher.Hash(refreshToken),
            FamilyId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExp
        };
        _db.RefreshTokens.Add(rtEntity);

        await _db.SaveChangesAsync();

        var (accessToken, accessExp) = _tokens.CreateAccessToken(user.Id, user.Email);

        return Created("", new
        {
            accessToken,
            refreshToken
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !_hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var (refreshToken, refreshExp) = _tokens.CreateRefreshToken();
        var rtEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _hasher.Hash(refreshToken),
            FamilyId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExp
        };
        _db.RefreshTokens.Add(rtEntity);

        await _db.SaveChangesAsync();

        var (accessToken, _) = _tokens.CreateAccessToken(user.Id, user.Email);

        return Ok(new { accessToken, refreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var tokens = await _db.RefreshTokens
            .Include(r => r.User)
            .Where(r => r.TokenHash != null)
            .ToListAsync();

        var stored = tokens.FirstOrDefault(r => BCrypt.Net.BCrypt.Verify(req.RefreshToken, r.TokenHash));

        if (stored == null || stored.ExpiresAt < DateTime.UtcNow || stored.RevokedAt != null)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        // Rotate
        stored.RevokedAt = DateTime.UtcNow;
        var (newRefreshToken, newExp) = _tokens.CreateRefreshToken();
        var newRt = new RefreshToken
        {
            UserId = stored.UserId,
            TokenHash = _hasher.Hash(newRefreshToken),
            FamilyId = stored.FamilyId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = newExp
        };
        _db.RefreshTokens.Add(newRt);

        var (accessToken, _) = _tokens.CreateAccessToken(stored.UserId, stored.User.Email);

        await _db.SaveChangesAsync();

        return Ok(new { accessToken, refreshToken = newRefreshToken });
    }
}
