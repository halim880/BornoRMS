using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Logistics;

/// <summary>
/// A delivery rider on the roster. Riders are managed records (not login accounts) that a dispatcher
/// assigns to delivery orders. Never hard-deleted — retire via <see cref="IsActive"/> so historical
/// deliveries keep their rider reference.
/// </summary>
public class Rider : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string? Vehicle { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Rider() { }

    public static Rider Create(string name, string phone, string? vehicle = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Rider name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Rider phone is required.", nameof(phone));

        return new Rider
        {
            Name = name.Trim(),
            Phone = phone.Trim(),
            Vehicle = string.IsNullOrWhiteSpace(vehicle) ? null : vehicle.Trim(),
            IsActive = true
        };
    }

    public void Update(string name, string phone, string? vehicle)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Rider name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Rider phone is required.", nameof(phone));
        Name = name.Trim();
        Phone = phone.Trim();
        Vehicle = string.IsNullOrWhiteSpace(vehicle) ? null : vehicle.Trim();
    }

    public void SetActive(bool active) => IsActive = active;
}
