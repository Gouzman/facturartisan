using FacturArtisan.Api.Data;
using FacturArtisan.Api.DTOs.Auth;
using FacturArtisan.Api.Models;
using FacturArtisan.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokenService;

    public AuthController(AppDbContext db, JwtTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email et mot de passe requis");

        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists) return Conflict("Email déjà utilisé");

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Email, user.CreatedAt });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email et mot de passe requis");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized("Identifiants invalides");

        var ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!ok) return Unauthorized("Identifiants invalides");

        var (accessToken, accessExpiresAt) = _tokenService.CreateAccessToken(user.Id, user.Email);
        var (refreshToken, refreshHash, refreshExpiresAt) = _tokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpiresAt
        });

        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token requis");

        var hash = JwtTokenService.HashRefreshToken(request.RefreshToken);

        var stored = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored == null) return Unauthorized("Refresh token invalide");
        if (stored.IsRevoked || stored.IsExpired) return Unauthorized("Refresh token expiré/révoqué");

        // Rotation: on révoque l'ancien token et on en émet un nouveau
        stored.RevokedAt = DateTime.UtcNow;

        var user = stored.User;
        var (accessToken, accessExpiresAt) = _tokenService.CreateAccessToken(user.Id, user.Email);
        var (refreshToken, refreshHash, refreshExpiresAt) = _tokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpiresAt
        });

        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token requis");

        var userIdStr = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var hash = JwtTokenService.HashRefreshToken(request.RefreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.TokenHash == hash);

        if (stored == null) return NoContent();

        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
