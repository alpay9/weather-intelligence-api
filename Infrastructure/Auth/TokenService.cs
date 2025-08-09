using Application.Abstractions.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Auth;
public class TokenService(IConfiguration config) : ITokenService
{
    public (string accessToken, DateTime expiresAt) CreateAccessToken(Guid userId, string email)
    {
        var issuer = config["Jwt:Issuer"]!;
        var audience = config["Jwt:Audience"]!;
        var lifetimeMinutes = int.Parse(config["Jwt:AccessTokenMinutes"]!);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT_SECRET"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email)
        };

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(lifetimeMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expires);
    }

    public (string refreshToken, DateTime expiresAt) CreateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(randomBytes);
        var expires = DateTime.UtcNow.AddDays(7);
        return (token, expires);
    }
}
