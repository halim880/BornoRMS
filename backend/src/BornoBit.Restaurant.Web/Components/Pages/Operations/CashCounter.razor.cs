using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using BornoBit.Restaurant.Web.Components.Shared;
using BornoBit.Restaurant.Web.Services.Printing;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Operations;

public partial class CashCounter : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;
    [Inject] private IReceiptPrintService PrintService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private CashSummaryDto? _summary;
    private List<OrderListItemDto> _pending = new();
    private List<OrderListItemDto> _paidToday = new();
    private Guid? _printingId;

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _summary = await Mediator.Send(new GetCashSummaryQuery());

            var pending = await Mediator.Send(new GetOrdersQuery(
                IsPaid: false, ExcludeCancelled: true, PageSize: 200));
            _pending = pending.Items.ToList();

            var paid = await Mediator.Send(new GetOrdersQuery(
                IsPaid: true, PageSize: 200));
            var todayUtc = DateTime.UtcNow.Date;
            _paidToday = paid.Items.Where(o => o.PaidAtUtc?.Date == todayUtc).ToList();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load cash counter: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task TakePaymentAsync(OrderListItemDto o)
    {
        var result = await DialogService.ShowAsync<BillDialog, Guid>(
            o.Id,
            new BoDialogOptions { Title = $"Bill · {o.OrderNumber}", Width = "520px" });

        // BillDialog returns true when discount/payment changed something.
        if (!result.Cancelled && result.Data is true)
            await ReloadAsync();
    }

    private async Task ReprintAsync(OrderListItemDto o)
    {
        _printingId = o.Id;
        try
        {
            var result = await PrintService.PrintReceiptAsync(o.Id, isReprint: true);
            if (result.Success) ToastService.ShowSuccess(result.Message);
            else ToastService.ShowWarning(result.Message);
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally
        {
            _printingId = null;
        }
    }

    private static string MethodIcon(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Money",
        PaymentMethod.Card => "Wallet",
        PaymentMethod.Mobile => "ReceiptMoney",
        _ => "Money"
    };

    // (chip background, icon/text color) — mirrors the SalesReport stat-card tints.
    private static (string Bg, string Color) MethodTint(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => ("color-mix(in srgb, var(--bo-success) 14%, #fff)", "var(--bo-success)"),
        PaymentMethod.Card => ("color-mix(in srgb, var(--bo-info) 14%, #fff)", "var(--bo-info)"),
        PaymentMethod.Mobile => ("color-mix(in srgb, var(--bo-primary) 14%, #fff)", "var(--bo-primary)"),
        _ => ("var(--bo-bg-soft)", "var(--bo-text-muted)")
    };

    private static string MethodTone(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "success",
        PaymentMethod.Card => "info",
        PaymentMethod.Mobile => "primary",
        _ => "neutral"
    };

    private static string StatusTone(OrderStatus status) => status switch
    {
        OrderStatus.Placed => "warning",
        OrderStatus.Confirmed => "info",
        OrderStatus.Preparing => "primary",
        OrderStatus.Ready => "success",
        OrderStatus.Served => "success",
        OrderStatus.Completed => "neutral",
        OrderStatus.Cancelled => "danger",
        _ => "neutral"
    };
}
