namespace BornoBit.Restaurant.Domain.Ordering;

public enum OrderStatus
{
    Placed = 0,
    Confirmed = 1,
    Preparing = 2,
    Ready = 3,
    Served = 4,
    Completed = 5,
    Cancelled = 99
}
