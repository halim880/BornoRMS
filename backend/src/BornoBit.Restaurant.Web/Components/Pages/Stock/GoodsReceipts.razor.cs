using BornoBit.Restaurant.Application.Inventory.Purchases;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class GoodsReceipts : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private bool _posting;
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
        _posting = true;
        try
        {
            await Mediator.Send(new PostGoodsReceiptCommand(g.Id));
            ToastService.ShowSuccess($"{g.GrnNumber} posted — stock updated.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _posting = false; }
    }
}
