using BornoBit.Restaurant.Application.Inventory.Items;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class LowStock : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<InventoryItemDto> _items = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _items = (await Mediator.Send(new GetLowStockItemsQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load low-stock items: {ex.Message}"; }
        finally { _loading = false; }
    }
}
