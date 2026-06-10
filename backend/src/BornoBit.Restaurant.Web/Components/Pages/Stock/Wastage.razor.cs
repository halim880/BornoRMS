using BornoBit.Restaurant.Application.Inventory.Items;
using BornoBit.Restaurant.Application.Inventory.Movements;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class Wastage : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private bool _busy;
    private string? _loadError;
    private string? _error;

    private List<InventoryItemDto> _items = new();
    private List<StockMovementDto> _recent = new();

    private InventoryItemDto? _selectedItem;
    private decimal _qty;
    private string? _reason;
    private string _unitCode = "";

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true; _loadError = null;
        try
        {
            var items = await Mediator.Send(new GetInventoryItemsQuery(PageSize: 200, IncludeInactive: false));
            _items = items.Items.ToList();
            await ReloadRecentAsync();
        }
        catch (Exception ex) { _loadError = $"Failed to load: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ReloadRecentAsync()
    {
        var recent = await Mediator.Send(new GetStockMovementsQuery(MovementType: StockMovementType.WastageOut, PageSize: 20));
        _recent = recent.Items.ToList();
    }

    private void OnItemChanged(InventoryItemDto? item)
    {
        _selectedItem = item;
        _unitCode = item?.UnitCode ?? "";
    }

    private async Task SubmitAsync()
    {
        _error = null;
        if (_selectedItem is null) { _error = "Select an item."; return; }
        if (string.IsNullOrWhiteSpace(_reason)) { _error = "A reason is required."; return; }

        _busy = true;
        try
        {
            await Mediator.Send(new RecordWastageCommand(_selectedItem.Id, _qty, _reason!.Trim()));
            ToastService.ShowSuccess($"Wastage recorded for '{_selectedItem.Name}'.");
            _qty = 0;
            _reason = null;
            await ReloadRecentAsync();
        }
        catch (ValidationException vex) { _error = string.Join("; ", vex.Errors.Select(e => e.ErrorMessage)); }
        catch (Exception ex) { _error = ex.Message; }
        finally { _busy = false; }
    }

    private static string ItemLabel(InventoryItemDto i) => $"{i.Name} ({i.QtyOnHand:0.###} {i.UnitCode} on hand)";
}
