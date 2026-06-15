using BornoBit.Restaurant.Application.Accounting.Drawers;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using BornoBit.Restaurant.Web.Components.Pages.Operations.Dialogs;
using BornoBit.Restaurant.Web.Hubs;
using BornoBit.Restaurant.Web.Services.Dashboard;
using BornoBit.Restaurant.Web.Services.Printing;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BornoBit.Restaurant.Web.Components.Pages.Operations;

public partial class CashCounter : ComponentBase, IAsyncDisposable
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;
    [Inject] private IReceiptPrintService PrintService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IBusinessClock Clock { get; set; } = default!;
    [Inject] private IDashboardNotifier Notifier { get; set; } = default!;

    private bool _loading = true;
    private string? _error;

    private DailySummaryDto? _summary;
    private DrawerDto? _drawer;
    private PagedResult<CashCounterRowDto> _board = new(Array.Empty<CashCounterRowDto>(), 1, BoardPageSize, 0);
    private PagedResult<PaymentLedgerRowDto> _ledger = new(Array.Empty<PaymentLedgerRowDto>(), 1, LedgerPageSize, 0);
    private IReadOnlyList<BillingRequestRowDto> _billingRequests = Array.Empty<BillingRequestRowDto>();

    private readonly Dictionary<Guid, OrderDetailDto?> _expanded = new();
    private Guid? _printingId;

    // Filters — seeded to the business "today" in OnInitializedAsync (Clock isn't injected yet at field-init time).
    private DateOnly _date;
    private PaymentStatus? _statusFilter;
    private OrderType? _typeFilter;
    private string? _waiterFilter;

    private const int BoardPageSize = 25;
    private const int LedgerPageSize = 25;
    private int _boardPage = 1;
    private int _ledgerPage = 1;

    private HubConnection? _hub;

    protected override async Task OnInitializedAsync()
    {
        _date = Clock.Today;
        await ReloadAsync();
        await ConnectHubAsync();
    }

    private async Task ConnectHubAsync()
    {
        try
        {
            _hub = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/hubs/dashboard"))
                .WithAutomaticReconnect()
                .Build();

            _hub.On<string>(DashboardHub.ChangedEvent, async _ =>
                await InvokeAsync(async () => { await ReloadAsync(); StateHasChanged(); }));

            await _hub.StartAsync();
        }
        catch { /* real-time is best-effort; manual refresh still works */ }
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _summary = await Mediator.Send(new GetDailySummaryQuery(_date));
            _drawer = await Mediator.Send(new GetCurrentDrawerQuery());
            _billingRequests = await Mediator.Send(new GetBillingRequestsQuery());
            await LoadBoardAsync();
            await LoadLedgerAsync();
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

    private async Task LoadBoardAsync()
    {
        _board = await Mediator.Send(new GetCashCounterBoardQuery(
            Date: _date, Waiter: _waiterFilter, OrderType: _typeFilter, PaymentStatus: _statusFilter,
            Page: _boardPage, PageSize: BoardPageSize));
    }

    private async Task LoadLedgerAsync()
    {
        _ledger = await Mediator.Send(new GetPaymentLedgerQuery(Date: _date, Page: _ledgerPage, PageSize: LedgerPageSize));
    }

    // String-backed filter selects (avoids nullable-enum generic friction with BoSelect).
    private static readonly string[] StatusItems = { "All", "Pending", "Partial", "Paid", "Refunded" };
    private static readonly string[] TypeItems = { "All", "DineIn", "Takeaway", "Delivery", "Collection" };
    private string _statusItem = "All";
    private string _typeItem = "All";

    private Task OnStatusChanged(string? value)
    {
        _statusItem = value ?? "All";
        _statusFilter = _statusItem switch
        {
            "Pending" => PaymentStatus.Pending,
            "Partial" => PaymentStatus.PartiallyPaid,
            "Paid" => PaymentStatus.Paid,
            "Refunded" => PaymentStatus.Refunded,
            _ => null
        };
        return OnFilterChanged();
    }

    private Task OnTypeChanged(string? value)
    {
        _typeItem = value ?? "All";
        _typeFilter = Enum.TryParse<OrderType>(_typeItem, out var t) ? t : null;
        return OnFilterChanged();
    }

    private Task OnWaiterChanged(string? value)
    {
        _waiterFilter = string.IsNullOrWhiteSpace(value) ? null : value;
        return OnFilterChanged();
    }

    private Task OnFilterChanged()
    {
        _boardPage = 1;
        return LoadBoardAsync();
    }

    private Task OnDateChanged(DateOnly? d)
    {
        _date = d ?? Clock.Today;
        _boardPage = 1; _ledgerPage = 1;
        return ReloadAsync();
    }

    private Task BoardPrev() { if (_boardPage > 1) { _boardPage--; return LoadBoardAsync(); } return Task.CompletedTask; }
    private Task BoardNext() { if (_boardPage < _board.TotalPages) { _boardPage++; return LoadBoardAsync(); } return Task.CompletedTask; }
    private Task LedgerPrev() { if (_ledgerPage > 1) { _ledgerPage--; return LoadLedgerAsync(); } return Task.CompletedTask; }
    private Task LedgerNext() { if (_ledgerPage < _ledger.TotalPages) { _ledgerPage++; return LoadLedgerAsync(); } return Task.CompletedTask; }

    private async Task ToggleExpandAsync(Guid orderId)
    {
        if (_expanded.Remove(orderId)) return;
        _expanded[orderId] = null; // show loading
        try { _expanded[orderId] = await Mediator.Send(new GetOrderQuery(orderId)); }
        catch (Exception ex) { ToastService.ShowError(ex.Message); _expanded.Remove(orderId); }
    }

    private async Task SettleAsync(CashCounterRowDto row)
    {
        var result = await DialogService.ShowAsync<Dialogs.SettlementDialog, Guid>(
            row.OrderId,
            new BoDialogOptions { Title = $"Settle · {row.OrderNumber}", Width = "640px" });

        if (!result.Cancelled && result.Data is true)
            await ReloadAsync();
    }

    private async Task SettleRequestAsync(BillingRequestRowDto row)
    {
        var result = await DialogService.ShowAsync<Dialogs.SessionSettleDialog, Guid>(
            row.SessionId,
            new BoDialogOptions { Title = $"Settle · Table {row.TableNumber}", Width = "560px" });

        if (!result.Cancelled && result.Data is true)
        {
            await Notifier.NotifyAsync(DashboardScopes.Sessions);
            await ReloadAsync();
        }
    }

    private string WaitingFor(DateTime requestedAtUtc)
    {
        var mins = (int)Math.Max(0, (DateTime.UtcNow - requestedAtUtc).TotalMinutes);
        return mins < 60 ? $"{mins} min" : $"{mins / 60}h {mins % 60}m";
    }

    private async Task OpenDrawerDialogAsync()
    {
        var result = await DialogService.ShowAsync<Dialogs.DrawerDialog, bool>(
            false, new BoDialogOptions { Title = "Cash drawer", Width = "460px" });
        if (!result.Cancelled && result.Data is true)
            await ReloadAsync();
    }

    private async Task OpenSettingsAsync()
    {
        var result = await DialogService.ShowAsync<Dialogs.BillingSettingsDialog, bool>(
            false, new BoDialogOptions { Title = "Billing settings", Width = "460px" });
        if (!result.Cancelled && result.Data is true)
            await ReloadAsync();
    }

    private async Task ReprintAsync(Guid orderId)
    {
        _printingId = orderId;
        try
        {
            var result = await PrintService.PrintReceiptAsync(orderId, isReprint: true);
            if (result.Success) ToastService.ShowSuccess(result.Message);
            else ToastService.ShowWarning(result.Message);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _printingId = null; }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null) await _hub.DisposeAsync();
    }

    // ----- display helpers -----

    private static string PaymentTone(PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "warning",
        PaymentStatus.PartiallyPaid => "info",
        PaymentStatus.Paid => "success",
        PaymentStatus.Refunded => "neutral",
        PaymentStatus.Cancelled => "danger",
        _ => "neutral"
    };

    private static string PaymentLabel(PaymentStatus s) => s switch
    {
        PaymentStatus.PartiallyPaid => "Partial",
        _ => s.ToString()
    };

    private static string MethodIcon(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Money",
        PaymentMethod.Card => "Wallet",
        PaymentMethod.Mobile => "ReceiptMoney",
        _ => "Money"
    };

    private static string MethodTone(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "success",
        PaymentMethod.Card => "info",
        PaymentMethod.Mobile => "primary",
        _ => "neutral"
    };
}
