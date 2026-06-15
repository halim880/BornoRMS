using BornoBit.Restaurant.Application.Inventory.Purchases;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class GoodsReceipts : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private bool _busy;
    private string? _error;
    private List<GoodsReceiptListItemDto> _rows = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetGoodsReceiptsQuery(PageSize: 100));
            _rows = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load goods receipts: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task PostAsync(GoodsReceiptListItemDto g)
    {
        _busy = true;
        try
        {
            await Mediator.Send(new PostGoodsReceiptCommand(g.Id));
            ToastService.ShowSuccess($"{g.GrnNumber} posted — stock updated.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _busy = false; }
    }

    private async Task DeleteAsync(GoodsReceiptListItemDto g)
    {
        var ok = await DialogService.ConfirmAsync(
            "Delete goods receipt",
            $"Delete draft {g.GrnNumber}? This cannot be undone.",
            confirmLabel: "Delete", variant: "danger");
        if (!ok) return;

        _busy = true;
        try
        {
            await Mediator.Send(new DeleteGoodsReceiptCommand(g.Id));
            ToastService.ShowSuccess($"{g.GrnNumber} deleted.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _busy = false; }
    }
}
