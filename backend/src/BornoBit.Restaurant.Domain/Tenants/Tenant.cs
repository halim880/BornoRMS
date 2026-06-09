using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Tenants;

public class Tenant : BaseEntity, IAuditableEntity, ISoftDelete
{
    public string Name { get; private set; } = default!;
    public string Subdomain { get; private set; } = default!;
    public string ContactEmail { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public DateTime? LicenseExpiresOnUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    private Tenant() { }

    public static Tenant Create(string name, string subdomain, string contactEmail, DateTime? licenseExpiresOnUtc = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tenant name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(subdomain)) throw new ArgumentException("Subdomain is required.", nameof(subdomain));
        if (string.IsNullOrWhiteSpace(contactEmail)) throw new ArgumentException("Contact email is required.", nameof(contactEmail));

        return new Tenant
        {
            Name = name.Trim(),
            Subdomain = subdomain.Trim().ToLowerInvariant(),
            ContactEmail = contactEmail.Trim().ToLowerInvariant(),
            LicenseExpiresOnUtc = licenseExpiresOnUtc,
            IsActive = true
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Tenant name is required.", nameof(newName));
        Name = newName.Trim();
    }

    public void UpdateDetails(string name, string contactEmail, DateTime? licenseExpiresOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tenant name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(contactEmail)) throw new ArgumentException("Contact email is required.", nameof(contactEmail));

        Name = name.Trim();
        ContactEmail = contactEmail.Trim().ToLowerInvariant();
        LicenseExpiresOnUtc = licenseExpiresOnUtc;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void SetLicenseExpiry(DateTime? expiresOnUtc) => LicenseExpiresOnUtc = expiresOnUtc;
}
