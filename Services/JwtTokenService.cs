using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FacturArtisan.Api.Services;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string token, DateTime expiresAtUtc) CreateAccessToken(Guid userId, string email)
    {
        var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
        var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");

        if (key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters");

        var expiresAt = DateTime.UtcNow.AddHours(1);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }

    public (string refreshToken, string refreshTokenHash, DateTime expiresAtUtc) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = Convert.ToBase64String(bytes);
        var hash = HashRefreshToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(30);
        return (refreshToken, hash, expiresAt);
    }

    public static string HashRefreshToken(string refreshToken)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = sha.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
