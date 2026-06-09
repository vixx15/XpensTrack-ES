using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UserManagement.Services;

public class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(string userId, string email, string firstName, string lastName,
        string defaultCurrencyCode, List<string> secondaryCurrencies)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var jwtSecret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>{
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.GivenName, firstName),
            new(JwtRegisteredClaimNames.FamilyName, lastName),
            new("defaultCurrency", defaultCurrencyCode),
        };
        claims.AddRange(secondaryCurrencies.Select(currency => new Claim("secondaryCurrencies", currency)));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}