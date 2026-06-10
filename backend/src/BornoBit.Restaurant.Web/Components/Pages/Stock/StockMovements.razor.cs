using BornoBit.Restaurant.Application.Inventory.Items;
using BornoBit.Restaurant.Application.Inventory.Movements;
using BornoBit.Restaurant.Domain.Inventory;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class StockMovements : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<StockMovementDto> _rows = new();
    private List<InventoryItemDto> _filterItems = new();
    private InventoryItemDto? _selectedItem;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var items = await Mediator.Send(new GetInventoryItemsQuery(PageSize: 200));
            _filterItems = items.Items.ToList();
            await ReloadAsync();
        }
        catch (Exception ex) { _error = $"Failed to load: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task OnFilterChanged(InventoryItemDto? item)
    {
        _selectedItem = item;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _error = null;
        try
        {
            var result = await Mediator.Send(new GetStockMovementsQuery(ItemId: _selectedItem?.Id, PageSize: 100));
            _rows = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load ledger: {ex.Message}"; }
    }

    private static string Label(StockMovementType t) => t switch
    {
        StockMovementType.OpeningBalance => "Opening",
        StockMovementType.PurchaseIn => "Purchase",
        StockMovementType.WastageOut => "Wastage",
        StockMovementType.AdjustmentIn => "Adjust +",
        StockMovementType.AdjustmentOut => "Adjust −",
        StockMovementType.ConsumptionOut => "Consumed",
        _ => t.ToString()
    };

    private static string ToneFor(StockMovementType t) => t switch
    {
        StockMovementType.PurchaseIn => "success",
        StockMovementType.WastageOut => "danger",
        StockMovementType.AdjustmentIn => "info",
        StockMovementType.AdjustmentOut => "warning",
        StockMovementType.ConsumptionOut => "neutral",
        _ => "neutral"
    };
}
