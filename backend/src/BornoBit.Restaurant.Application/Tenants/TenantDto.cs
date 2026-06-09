namespace BornoBit.Restaurant.Application.Tenants;

public record TenantDto(
    Guid Id,
    string Name,
    string Subdomain,
    string ContactEmail,
    bool IsActive,
    DateTime? LicenseExpiresOnUtc,
    DateTime CreatedAtUtc
);
