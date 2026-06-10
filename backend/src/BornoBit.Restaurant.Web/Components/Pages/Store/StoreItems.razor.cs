using BornoBit.Restaurant.Application.Store.Items;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreItems : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<StoreItemDto> _items = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetStoreItemsQuery(PageSize: 200));
            _items = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load store items: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var model = new StoreItemFormModel();
        var result = await DialogService.ShowAsync<StoreItemFormDialog, StoreItemFormModel>(model, new BoDialogOptions
        {
            Title = "New store item",
            Width = "640px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreItemFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Store item '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(StoreItemDto i)
    {
        var model = new StoreItemFormModel
        {
            Id = i.Id,
            Code = i.Code,
            Name = i.Name,
            BanglaName = i.BanglaName,
            StoreCategoryId = i.StoreCategoryId,
            BaseUnitId = i.BaseUnitId,
            ReorderLevel = i.ReorderLevel,
            ReorderQty = i.ReorderQty,
            IsPerishable = i.IsPerishable,
            PackSize = i.PackSize,
            PackNote = i.PackNote
        };
        var result = await DialogService.ShowAsync<StoreItemFormDialog, StoreItemFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit · {i.Name}",
            Width = "640px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreItemFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Store item updated.");
            await ReloadAsync();
        }
    }

    private async Task ShowAdjustAsync(StoreItemDto i)
    {
        var model = new StoreAdjustModel
        {
            ItemId = i.Id,
            ItemName = i.Name,
            UnitCode = i.UnitCode,
            CurrentQty = i.QtyOnHand,
            CountedQty = i.QtyOnHand
        };
        var result = await DialogService.ShowAsync<StoreAdjustDialog, StoreAdjustModel>(model, new BoDialogOptions
        {
            Title = $"Adjust stock · {i.Name}",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreAdjustModel saved && saved.Saved)
        {
            ToastService.ShowSuccess($"Stock adjusted for '{i.Name}'.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(StoreItemDto i, bool active)
    {
        try
        {
            await Mediator.Send(new SetStoreItemActiveCommand(i.Id, active));
            ToastService.ShowSuccess($"'{i.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
