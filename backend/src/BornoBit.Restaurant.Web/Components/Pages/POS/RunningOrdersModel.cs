using BornoBit.Restaurant.Application.Ordering.Pos;

namespace BornoBit.Restaurant.Web.Components.Pages.POS;

public class RunningOrdersModel
{
    public IReadOnlyList<ActiveOrderDto> Orders { get; set; } = Array.Empty<ActiveOrderDto>();
    public Guid? ActiveOrderId { get; set; }
}
