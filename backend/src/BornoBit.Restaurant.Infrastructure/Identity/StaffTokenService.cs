using BornoBit.Restaurant.Application.Common.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BornoBit.Restaurant.Infrastructure.Identity;

public class StaffTokenService : IStaffTokenService
{
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;

    public StaffTokenService(IConfiguration configuration, TimeProvider timeProvider)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;
    }

    public StaffTokenResult IssueAccessToken(Guid userId, string userName, string? email, string fullName, IEnumerable<string> roles)
    {
        var section = _configuration.GetSection("Jwt");
        var signingKey = section["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");
        var issuer = section["Issuer"];
        var audience = section["Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing.");
        var minutes = int.TryParse(section["AccessTokenMinutes"], out var m) ? m : 30;

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresUtc = nowUtc.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("typ", "staff"),
            new(ClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Name, fullName)
        };

        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(ClaimTypes.Email, email));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresUtc,
            signingCredentials: creds);

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);
        return new StaffTokenResult(serialized, expiresUtc);
    }
}
