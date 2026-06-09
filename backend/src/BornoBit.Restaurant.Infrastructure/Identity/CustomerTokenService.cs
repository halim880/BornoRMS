using BornoBit.Restaurant.Application.Common.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BornoBit.Restaurant.Infrastructure.Identity;

public class CustomerTokenService : ICustomerTokenService
{
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;

    public CustomerTokenService(IConfiguration configuration, TimeProvider timeProvider)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;
    }

    public CustomerTokenResult IssueAccessToken(Guid customerId, string phone, string? fullName)
    {
        var section = _configuration.GetSection("Jwt");
        var signingKey = section["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");
        var issuer = section["Issuer"];
        var audience = section["CustomerAudience"]
            ?? throw new InvalidOperationException("Jwt:CustomerAudience is missing.");
        var minutes = int.TryParse(section["CustomerAccessMinutes"], out var m) ? m : 60;

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresUtc = nowUtc.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customerId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("typ", "customer"),
            new("phone", phone),
            new(JwtRegisteredClaimNames.Name, fullName ?? string.Empty)
        };

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
        return new CustomerTokenResult(serialized, expiresUtc);
    }
}
