using BornoBit.Restaurant.Application.Store.Purchases;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreGoodsReceipts : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private bool _posting;
    private string? _error;
    private List<StoreGoodsReceiptListItemDto> _rows = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetStoreGoodsReceiptsQuery(PageSize: 100));
            _rows = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load goods receipts: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task PostAsync(StoreGoodsReceiptListItemDto g)
    {
        _posting = true;
        try
        {
            await Mediator.Send(new PostStoreGoodsReceiptCommand(g.Id));
            ToastService.ShowSuccess($"{g.GrnNumber} posted — stock updated.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _posting = false; }
    }

    private async Task VoidAsync(StoreGoodsReceiptListItemDto g)
    {
        _posting = true;
        try
        {
            await Mediator.Send(new VoidStoreGoodsReceiptCommand(g.Id, null));
            ToastService.ShowSuccess($"{g.GrnNumber} voided — stock reversed.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _posting = false; }
    }

    private static string StatusTone(StoreGoodsReceiptStatus status) => status switch
    {
        StoreGoodsReceiptStatus.Posted => "success",
        StoreGoodsReceiptStatus.Voided => "neutral",
        _ => "warning"
    };
}
