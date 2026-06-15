using BornoBit.Restaurant.Application.Inventory.PurchaseOrders;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class PurchaseOrders : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private bool _busy;
    private string? _error;
    private List<PurchaseOrderListItemDto> _rows = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            var result = await Mediator.Send(new GetPurchaseOrdersQuery(PageSize: 100));
            _rows = result.Items.ToList();
        }
        catch (Exception ex) { _error = $"Failed to load purchase orders: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ApproveAsync(PurchaseOrderListItemDto p)
    {
        _busy = true;
        try
        {
            await Mediator.Send(new ApprovePurchaseOrderCommand(p.Id));
            ToastService.ShowSuccess($"{p.PoNumber} approved — ready to receive.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _busy = false; }
    }

    internal static string StatusTone(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => "neutral",
        PurchaseOrderStatus.Approved => "info",
        PurchaseOrderStatus.PartiallyReceived => "warning",
        PurchaseOrderStatus.Received => "success",
        PurchaseOrderStatus.Cancelled => "danger",
        _ => "neutral"
    };

    internal static string StatusLabel(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.PartiallyReceived => "Partially Received",
        _ => status.ToString()
    };
}
