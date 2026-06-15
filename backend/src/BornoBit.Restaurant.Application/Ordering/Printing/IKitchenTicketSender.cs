using BornoBit.Restaurant.Domain.Ordering;

namespace BornoBit.Restaurant.Application.Ordering.Printing;

/// <summary>
/// Transport for the kitchen order ticket (KOT). Implemented in the Web host by the print-agent sender;
/// the API host binds <see cref="NullKitchenTicketSender"/> (no print agent there). The order's
/// <see cref="Order.Lines"/> must be loaded — the implementation builds the ticket payload from the entity.
/// </summary>
public interface IKitchenTicketSender
{
    /// <summary>Dispatches the ticket to the kitchen printer. Returns true when the agent acknowledges it.</summary>
    Task<bool> SendAsync(Order order, CancellationToken cancellationToken = default);
}
