using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Store;

/// <summary>A store/warehouse goods supplier — wholesaler or distributor. Isolated from the POS Supplier.</summary>
public class StoreSupplier : AuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public int PaymentTermsDays { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    private StoreSupplier() { }

    public static StoreSupplier Create(
        string code,
        string name,
        string? phone = null,
        string? address = null,
        int paymentTermsDays = 0,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (paymentTermsDays < 0) throw new ArgumentOutOfRangeException(nameof(paymentTermsDays));

        return new StoreSupplier
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Phone = Trim(phone),
            Address = Trim(address),
            PaymentTermsDays = paymentTermsDays,
            Notes = Trim(notes),
            IsActive = true
        };
    }

    public void UpdateDetails(string name, string? phone, string? address, int paymentTermsDays, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (paymentTermsDays < 0) throw new ArgumentOutOfRangeException(nameof(paymentTermsDays));

        Name = name.Trim();
        Phone = Trim(phone);
        Address = Trim(address);
        PaymentTermsDays = paymentTermsDays;
        Notes = Trim(notes);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
