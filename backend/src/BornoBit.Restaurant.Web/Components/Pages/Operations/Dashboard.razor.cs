using BornoBit.Restaurant.Application.Operations.Dashboard;
using BornoBit.Restaurant.Application.Operations.Sessions;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Web.Hubs;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Operations;

public partial class Dashboard : ComponentBase, IAsyncDisposable
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private HubConnection? _hub;

    // KPIs + live sections
    private DashboardSummaryDto? _summary;
    private IReadOnlyList<TableOverviewRowDto> _tables = Array.Empty<TableOverviewRowDto>();
    private KitchenPerformanceDto? _kitchen;
    private PagedResult<LiveOrderRowDto> _orders = new(Array.Empty<LiveOrderRowDto>(), 1, 20, 0);
    private IReadOnlyList<CustomerRequestRowDto> _requests = Array.Empty<CustomerRequestRowDto>();
    private InventoryAlertsDto? _inventory;
    private IReadOnlyList<StaffActivityRowDto> _staff = Array.Empty<StaffActivityRowDto>();

    // Analytics sections (range-driven)
    private IReadOnlyList<HourlySalesDto> _hourly = Array.Empty<HourlySalesDto>();
    private IReadOnlyList<CategorySalesDto> _byCategory = Array.Empty<CategorySalesDto>();
    private IReadOnlyList<TopItemRowDto> _topItems = Array.Empty<TopItemRowDto>();
    private RevenueBreakdownDto? _revenue;

    private bool _loading = true;
    private string? _error;

    // Filters
    private DashboardRange _range = DashboardRange.Today;
    private DateTime _customFrom = DateTime.UtcNow.Date;
    private DateTime _customTo = DateTime.UtcNow.Date;
    private OrderStatus? _orderFilter;
    private TableOverviewRowDto? _selectedTable;
    private string? _tableMessage;

    // Reservation quick form
    private bool _showReservation;
    private Guid _resTableId;
    private string _resName = string.Empty;
    private string? _resPhone;
    private int _resParty = 2;
    private DateTime _resWhen = DateTime.UtcNow.Date.AddHours(19);
    private string? _resMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadAllAsync();
        _loading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            _hub = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/hubs/dashboard"))
                .WithAutomaticReconnect()
                .Build();

            _hub.On<string>(DashboardHub.ChangedEvent, async _ =>
                await InvokeAsync(async () =>
                {
                    await LoadLiveAsync();
                    StateHasChanged();
                }));

            await _hub.StartAsync();
        }
        catch
        {
            // Real-time is best-effort; the page still works as a manual-refresh dashboard without it.
        }
    }

    private (DateTime From, DateTime To) Window()
    {
        var today = DateTime.UtcNow.Date;
        return _range switch
        {
            DashboardRange.Today => (today, today),
            DashboardRange.Yesterday => (today.AddDays(-1), today.AddDays(-1)),
            DashboardRange.Last7Days => (today.AddDays(-6), today),
            DashboardRange.ThisMonth => (new DateTime(today.Year, today.Month, 1), today),
            _ => (_customFrom.Date, _customTo.Date)
        };
    }

    private async Task LoadAllAsync()
    {
        _error = null;
        try
        {
            await Task.WhenAll(LoadLiveAsync(), LoadAnalyticsAsync());
        }
        catch (Exception ex)
        {
            _error = $"Failed to load dashboard: {ex.Message}";
        }
    }

    private async Task LoadLiveAsync()
    {
        _summary = await Mediator.Send(new GetDashboardSummaryQuery());
        _tables = await Mediator.Send(new GetTableOverviewQuery());
        _kitchen = await Mediator.Send(new GetKitchenPerformanceQuery());
        _orders = await Mediator.Send(new GetLiveOrdersQuery(_orderFilter, _orders.Page, _orders.PageSize));
        _requests = await Mediator.Send(new GetCustomerRequestsQuery(CustomerRequestStatus.Pending));
        _inventory = await Mediator.Send(new GetInventoryAlertsQuery());
        _staff = await Mediator.Send(new GetStaffActivityQuery());
    }

    private async Task LoadAnalyticsAsync()
    {
        var (from, to) = Window();
        _hourly = await Mediator.Send(new GetSalesByHourQuery(from, to));
        _byCategory = await Mediator.Send(new GetSalesByCategoryQuery(from, to));
        _topItems = await Mediator.Send(new GetTopSellingItemsQuery(from, to, 8));
        _revenue = await Mediator.Send(new GetRevenueBreakdownQuery(from, to));
    }

    private async Task SetRange(DashboardRange range)
    {
        _range = range;
        await LoadAnalyticsAsync();
    }

    private Task OnCustomFrom(DateTime? d) { if (d.HasValue) { _customFrom = d.Value.Date; _range = DashboardRange.Custom; } return LoadAnalyticsAsync(); }
    private Task OnCustomTo(DateTime? d) { if (d.HasValue) { _customTo = d.Value.Date; _range = DashboardRange.Custom; } return LoadAnalyticsAsync(); }

    private async Task FilterOrders(OrderStatus? status)
    {
        _orderFilter = status;
        _orders = await Mediator.Send(new GetLiveOrdersQuery(_orderFilter, 1, _orders.PageSize));
    }

    private async Task OrdersPage(int page)
    {
        if (page < 1 || page > _orders.TotalPages) return;
        _orders = await Mediator.Send(new GetLiveOrdersQuery(_orderFilter, page, _orders.PageSize));
    }

    private async Task ResolveRequest(Guid id)
    {
        await Mediator.Send(new ResolveCustomerRequestCommand(id));
        _requests = await Mediator.Send(new GetCustomerRequestsQuery(CustomerRequestStatus.Pending));
        await NotifyAsync();
    }

    private void SelectTable(TableOverviewRowDto t)
    {
        _tableMessage = null;
        _selectedTable = _selectedTable?.TableId == t.TableId ? null : t;
    }

    private async Task ReleaseTableAsync(TableOverviewRowDto t)
    {
        _tableMessage = null;
        if (t.SessionId is not { } sessionId) return;
        try
        {
            await Mediator.Send(new CloseSessionCommand(sessionId, "Released from dashboard"));
            await LoadLiveAsync();
            _selectedTable = _tables.FirstOrDefault(x => x.TableId == t.TableId);
            await NotifyAsync();
        }
        catch (Exception ex)
        {
            _tableMessage = ex.Message;
        }
    }

    private void OpenReservation(Guid? tableId = null)
    {
        _resTableId = tableId ?? _tables.FirstOrDefault()?.TableId ?? Guid.Empty;
        _resMessage = null;
        _showReservation = true;
    }

    private async Task SubmitReservation()
    {
        _resMessage = null;
        try
        {
            await Mediator.Send(new CreateReservationCommand(_resTableId, _resName, _resPhone, _resParty, _resWhen, null));
            _showReservation = false;
            _resName = string.Empty; _resPhone = null; _resParty = 2;
            await LoadLiveAsync();
            await NotifyAsync();
        }
        catch (Exception ex)
        {
            _resMessage = ex.Message;
        }
    }

    private async Task NotifyAsync()
    {
        if (_hub is { State: HubConnectionState.Connected })
        {
            // Let other connected dashboards know; our own already refreshed.
        }
    }

    private Task PrintAsync() => Js.InvokeVoidAsync("window.print").AsTask();
    private void Go(string url) => Nav.NavigateTo(url);

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            try { await _hub.DisposeAsync(); } catch { /* ignore */ }
        }
    }

    // ---- View helpers ----
    private static string StatusTone(OrderStatus s) => s switch
    {
        OrderStatus.Placed => "neutral",
        OrderStatus.Confirmed => "info",
        OrderStatus.Preparing => "warning",
        OrderStatus.Ready => "primary",
        OrderStatus.Served => "info",
        OrderStatus.Completed => "success",
        OrderStatus.Cancelled => "danger",
        _ => "neutral"
    };

    private static string RequestLabel(CustomerRequestType t) => t switch
    {
        CustomerRequestType.CallWaiter => "Call Waiter",
        CustomerRequestType.RequestBill => "Request Bill",
        CustomerRequestType.NeedWater => "Need Water",
        CustomerRequestType.NeedTissue => "Need Tissue",
        _ => t.ToString()
    };

    private string Money(decimal v) => $"{v:#,##0.00} {_summary?.Currency ?? "Tk"}";
}
