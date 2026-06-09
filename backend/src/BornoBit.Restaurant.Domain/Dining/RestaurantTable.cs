using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Dining;

public class RestaurantTable : AuditableEntity
{
    public string TableNumber { get; private set; } = default!;
    public int Capacity { get; private set; }
    public bool IsActive { get; private set; } = true;

    private RestaurantTable() { }

    public static RestaurantTable Create(string tableNumber, int capacity)
    {
        if (string.IsNullOrWhiteSpace(tableNumber)) throw new ArgumentException("Table number is required.", nameof(tableNumber));
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");

        return new RestaurantTable
        {
            TableNumber = tableNumber.Trim(),
            Capacity = capacity,
            IsActive = true
        };
    }

    public void UpdateDetails(string tableNumber, int capacity)
    {
        if (string.IsNullOrWhiteSpace(tableNumber)) throw new ArgumentException("Table number is required.", nameof(tableNumber));
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        TableNumber = tableNumber.Trim();
        Capacity = capacity;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
