using BornoBit.Restaurant.Application.Dining.Queries;
using BornoBit.Restaurant.Domain.Ordering;

namespace BornoBit.Restaurant.Web.Components.Pages.POS;

/// <summary>
/// Dialog model for creating an empty POS order, or editing an open order's type/table/customer
/// when <see cref="EditOrderId"/> is set. The page reads CreatedOrderId back after a create.
/// </summary>
public class NewOrderModel
{
    public OrderType Type { get; set; } = OrderType.DineIn;
    public Guid? TableId { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public IReadOnlyList<TableDto> Tables { get; set; } = Array.Empty<TableDto>();

    /// <summary>Tables held by other open orders — rendered busy and unselectable.</summary>
    public IReadOnlySet<Guid> OccupiedTableIds { get; set; } = new HashSet<Guid>();

    /// <summary>When set, the dialog edits this order instead of creating a new one.</summary>
    public Guid? EditOrderId { get; set; }
    public string? EditOrderNumber { get; set; }

    public Guid? CreatedOrderId { get; set; }
    public string? CreatedOrderNumber { get; set; }
}
