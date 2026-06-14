using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Dining;

public enum ReservationStatus
{
    Booked = 0,
    Seated = 1,
    Cancelled = 2
}

/// <summary>
/// A lightweight table booking. Powers the dashboard "Reserved tables" count and the
/// "Create Reservation" quick action — intentionally not a full booking system.
/// </summary>
public class TableReservation : AuditableEntity
{
    public Guid RestaurantTableId { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public string? Phone { get; private set; }
    public int PartySize { get; private set; }
    public DateTime ReservedForUtc { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Booked;
    public string? Note { get; private set; }

    private TableReservation() { }

    public static TableReservation Create(Guid restaurantTableId, string customerName, string? phone, int partySize, DateTime reservedForUtc, string? note = null)
    {
        if (restaurantTableId == Guid.Empty) throw new ArgumentException("Table is required.", nameof(restaurantTableId));
        if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentException("Customer name is required.", nameof(customerName));
        if (partySize < 1) throw new ArgumentOutOfRangeException(nameof(partySize), "Party size must be at least 1.");

        return new TableReservation
        {
            RestaurantTableId = restaurantTableId,
            CustomerName = customerName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            PartySize = partySize,
            ReservedForUtc = reservedForUtc,
            Status = ReservationStatus.Booked,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }

    public void MarkSeated() => Status = ReservationStatus.Seated;
    public void Cancel() => Status = ReservationStatus.Cancelled;
}
