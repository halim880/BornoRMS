using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>A consuming department the store issues stock to (Kitchen, Bar, Bakery, Housekeeping…).
/// A cost centre, not a sub-store: issuing to a department records consumption against it but the
/// store keeps a single central stock balance (no per-department on-hand quantity).</summary>
public class StoreDepartment : AuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? BanglaName { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private StoreDepartment() { }

    public static StoreDepartment Create(string code, string name, int displayOrder = 0, string? banglaName = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Department code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Department name is required.", nameof(name));

        return new StoreDepartment
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            BanglaName = Trim(banglaName),
            Description = Trim(description),
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void UpdateDetails(string code, string name, int displayOrder, string? banglaName, string? description)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Department code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Department name is required.", nameof(name));
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        DisplayOrder = displayOrder;
        BanglaName = Trim(banglaName);
        Description = Trim(description);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
