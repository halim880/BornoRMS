using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Customers;

public class Customer : AuditableEntity
{
    /// <summary>Sentinel phone for the shared walk-in customer used by staff/waiter orders.</summary>
    public const string WalkInPhone = "WALK-IN";

    public string Phone { get; private set; } = default!;
    public string? FullName { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Customer() { }

    public static Customer Create(string phone, string? fullName = null)
    {
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required.", nameof(phone));

        return new Customer
        {
            Phone = phone.Trim(),
            FullName = Trim(fullName),
            IsActive = true
        };
    }

    public void UpdateName(string? fullName) => FullName = Trim(fullName);

    public void UpdateAddress(string? address) => Address = Trim(address);

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
