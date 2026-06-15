using BornoBit.Restaurant.Application.Kitchen.Commands;
using BornoBit.Restaurant.Application.Kitchen.Queries;
using BornoBit.Restaurant.Application.Operations.Dashboard;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using BornoBit.Restaurant.Web.Hubs;
using BornoBit.Restaurant.Web.Services.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Operations;

public partial class KitchenDisplay : ComponentBase, IAsyncDisposable
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private IBoToastService Toast { get; set; } = default!;
    [Inject] private IDashboardNotifier Notifier { get; set; } = default!;

    private HubConnection? _hub;
    private System.Threading.Timer? _ticker;
    private DotNetObjectReference<KitchenDisplay>? _selfRef;
    private ElementReference _pageEl;

    private IReadOnlyList<KitchenStationDto> _stations = Array.Empty<KitchenStationDto>();
    private KitchenBoardDto? _board;
    private KitchenPerformanceDto? _metrics;

    private bool _loading = true;
    private string? _error;

    // Filters
    private Guid? _stationId;            // null = All
    private OrderType? _typeFilter;
    private string? _tableFilter;
    private string? _searchOrder;

    // Live timer reference (UTC); the 1s tick advances it so cards recompute their elapsed colour.
    private DateTime _nowUtc = DateTime.UtcNow;

    // Sound + connection state
    private bool _muted;
    private bool _light;
    private bool _isFullscreen;
    private string _connState = "connecting";
    private bool _audioUnlocked;
    private HashSet<Guid>? _knownOrderIds;

    // Kitchen-notes inline editor
    private KitchenOrderCardDto? _editingCard;
    private string _editingNotes = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _stations = await Mediator.Send(new GetKitchenStationsQuery());
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load kitchen display: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _selfRef = DotNetObjectReference.Create(this);
        try { await Js.InvokeVoidAsync("kds.onFullscreenChange", _selfRef); } catch { /* best effort */ }

        try
        {
            var theme = await Js.InvokeAsync<string?>("kds.getTheme");
            if (theme == "light") { _light = true; await InvokeAsync(StateHasChanged); }
        }
        catch { /* best effort */ }

        // 1-second UI tick: only recomputes elapsed-time colours, no DB query.
        _ticker = new System.Threading.Timer(async _ =>
        {
            _nowUtc = DateTime.UtcNow;
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        try
        {
            _hub = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/hubs/dashboard"))
                .WithAutomaticReconnect()
                .Build();

            _hub.On<string>(DashboardHub.ChangedEvent, async _ =>
                await InvokeAsync(async () =>
                {
                    await ReloadAsync();
                    StateHasChanged();
                }));

            _hub.Reconnecting += _ => { _connState = "reconnecting"; return InvokeAsync(StateHasChanged); };
            _hub.Reconnected += _ => { _connState = "live"; return InvokeAsync(StateHasChanged); };
            _hub.Closed += _ => { _connState = "offline"; return InvokeAsync(StateHasChanged); };

            await _hub.StartAsync();
            _connState = "live";
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            _connState = "offline";
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ReloadAsync()
    {
        _error = null;
        _nowUtc = DateTime.UtcNow;
        _board = await Mediator.Send(new GetKitchenBoardQuery(
            StationId: _stationId,
            Type: _typeFilter,
            TableNumber: string.IsNullOrWhiteSpace(_tableFilter) ? null : _tableFilter,
            SearchOrderNumber: string.IsNullOrWhiteSpace(_searchOrder) ? null : _searchOrder));
        _metrics = await Mediator.Send(new GetKitchenPerformanceQuery());
        DetectNewOrdersAndChime();
    }

    private void DetectNewOrdersAndChime()
    {
        if (_board is null) return;
        var all = _board.Pending.Concat(_board.Preparing).Concat(_board.Ready).ToList();
        var currentIds = all.Select(c => c.Id).ToHashSet();

        if (_knownOrderIds is not null && !_muted)
        {
            var fresh = all.Where(c => !_knownOrderIds.Contains(c.Id)).ToList();
            if (fresh.Count > 0)
            {
                var fn = fresh.Any(c => c.IsPriority) ? "kds.playPriority" : "kds.playNewOrder";
                _ = Js.InvokeVoidAsync(fn);
            }
        }
        _knownOrderIds = currentIds;
    }

    // ---- Filters ----
    private async Task SelectStation(Guid? stationId)
    {
        _stationId = stationId;
        await ReloadAsync();
    }

    private async Task OnTypeFilter(ChangeEventArgs e)
    {
        _typeFilter = Enum.TryParse<OrderType>(e.Value?.ToString(), out var t) ? t : null;
        await ReloadAsync();
    }

    private async Task OnTableFilter(ChangeEventArgs e)
    {
        _tableFilter = e.Value?.ToString();
        await ReloadAsync();
    }

    private async Task OnSearch(ChangeEventArgs e)
    {
        _searchOrder = e.Value?.ToString();
        await ReloadAsync();
    }

    private async Task ClearFilters()
    {
        _typeFilter = null; _tableFilter = null; _searchOrder = null; _stationId = null;
        await ReloadAsync();
    }

    // ---- Order actions ----
    private async Task Accept(Guid orderId)
    {
        try
        {
            await Mediator.Send(new AcceptKitchenOrderCommand(orderId));
            await ReloadAsync();
            await Notifier.NotifyAsync(DashboardScopes.Orders);   // fan out + fire the kitchen ticket
            Toast.ShowSuccess("Order accepted — kitchen ticket sent.");
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
    }

    private async Task Advance(Guid orderId)
    {
        try
        {
            // Warn-but-allow: surface any ingredient shortfall before the order starts cooking (deducts stock).
            // Never blocks — the kitchen still advances the order.
            var availability = await Mediator.Send(
                new BornoBit.Restaurant.Application.Inventory.Consumption.GetOrderStockAvailabilityQuery(orderId));
            if (availability.HasShortages)
                Toast.ShowWarning(
                    "Low stock: " + string.Join(", ", availability.Shortages.Select(s => s.Name)),
                    title: "Stock warning");

            var newStatus = await Mediator.Send(new AdvanceKitchenOrderCommand(orderId));
            await ReloadAsync();
            await Notifier.NotifyAsync(DashboardScopes.Orders);   // fan out to cashier/waiter/table/dashboard
            await Notifier.NotifyAsync(DashboardScopes.Inventory); // StartPreparing deducts stock → refresh stock dashboard
            if (newStatus == OrderStatus.Ready)
                Toast.ShowSuccess("Order is ready — front of house notified.");
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
    }

    private async Task TogglePriority((Guid OrderId, bool Value) args)
    {
        try
        {
            await Mediator.Send(new ToggleOrderPriorityCommand(args.OrderId, args.Value));
            await ReloadAsync();
            await Notifier.NotifyAsync(DashboardScopes.Orders);
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
    }

    private void EditNotes(KitchenOrderCardDto card)
    {
        _editingCard = card;
        _editingNotes = card.KitchenNotes ?? string.Empty;
    }

    private void CancelNotes() => _editingCard = null;

    private async Task SaveNotes()
    {
        if (_editingCard is null) return;
        try
        {
            await Mediator.Send(new UpdateKitchenNotesCommand(_editingCard.Id, _editingNotes));
            _editingCard = null;
            await ReloadAsync();
            await Notifier.NotifyAsync(DashboardScopes.Orders);
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
    }

    // ---- Sound / fullscreen ----
    private async Task ToggleMute()
    {
        _muted = !_muted;
        await Js.InvokeVoidAsync("kds.setMuted", _muted);
        if (!_audioUnlocked)
        {
            await Js.InvokeVoidAsync("kds.unlockAudio");
            _audioUnlocked = true;
        }
    }

    private async Task ToggleFullscreen()
    {
        if (!_audioUnlocked)
        {
            await Js.InvokeVoidAsync("kds.unlockAudio");
            _audioUnlocked = true;
        }
        if (_isFullscreen)
            await Js.InvokeVoidAsync("kds.exitFullscreen");
        else
            await Js.InvokeVoidAsync("kds.enterFullscreen", _pageEl);
    }

    private async Task ToggleTheme()
    {
        _light = !_light;
        try { await Js.InvokeVoidAsync("kds.setTheme", _light ? "light" : "dark"); } catch { /* best effort */ }
    }

    [JSInvokable]
    public Task OnFullscreenChanged(bool isFullscreen)
    {
        _isFullscreen = isFullscreen;
        return InvokeAsync(StateHasChanged);
    }

    // ---- View helpers ----
    private int PendingCount => _board?.Pending.Count ?? 0;
    private int PreparingCount => _board?.Preparing.Count ?? 0;
    private int ReadyCount => _board?.Ready.Count ?? 0;

    private string ConnLabel => _connState switch
    {
        "live" => "Live",
        "reconnecting" => "Reconnecting…",
        "offline" => "Offline",
        _ => "Connecting…"
    };

    public async ValueTask DisposeAsync()
    {
        if (_ticker is not null) await _ticker.DisposeAsync();
        if (_hub is not null)
        {
            try { await _hub.DisposeAsync(); } catch { /* ignore */ }
        }
        try { await Js.InvokeVoidAsync("kds.dispose"); } catch { /* ignore */ }
        _selfRef?.Dispose();
    }
}
