using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ExoAuth.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _accessTokenExpiration;
    private readonly TimeSpan _refreshTokenExpiration;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenService(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");

        _secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("JWT Secret is not configured");
        _issuer = jwtSection["Issuer"] ?? "ExoAuth";
        _audience = jwtSection["Audience"] ?? "ExoAuth";

        var accessMinutes = int.Parse(jwtSection["AccessTokenExpirationMinutes"] ?? "15");
        var refreshDays = int.Parse(jwtSection["RefreshTokenExpirationDays"] ?? "30");

        _accessTokenExpiration = TimeSpan.FromMinutes(accessMinutes);
        _refreshTokenExpiration = TimeSpan.FromDays(refreshDays);
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public TimeSpan AccessTokenExpiration => _accessTokenExpiration;
    public TimeSpan RefreshTokenExpiration => _refreshTokenExpiration;

    public string GenerateAccessToken(Guid userId, string email, UserType userType, IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("type", userType.ToString().ToLowerInvariant())
        };

        // Add permissions as individual claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_accessTokenExpiration),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public TokenClaims? ValidateAccessToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
                return null;

            var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!);
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value!;
            var typeString = principal.FindFirst("type")?.Value!;
            var userType = Enum.Parse<UserType>(typeString, ignoreCase: true);
            var permissions = principal.FindAll("permission").Select(c => c.Value).ToList();

            return new TokenClaims(
                userId,
                email,
                userType,
                permissions,
                jwtToken.ValidTo
            );
        }
        catch
        {
            return null;
        }
    }
}
