using BornoBit.Restaurant.Application.Dining.Queries;
using BornoBit.Restaurant.Application.Operations.Dashboard;
using BornoBit.Restaurant.Application.Operations.Sessions;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Application.ProductCategories;
using BornoBit.Restaurant.Application.Products;
using BornoBit.Restaurant.Application.Users;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using BornoBit.Restaurant.Web.Components.Pages.Waiter.Dialogs;
using BornoBit.Restaurant.Web.Hubs;
using BornoBit.Restaurant.Web.Services.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages;

public partial class WaiterOrders : ComponentBase, IAsyncDisposable
{
    [Inject] private ISender Sender { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IDashboardNotifier Notifier { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    private enum WaiterTab { Floor, TakeOrder, Ready, Requests }

    private sealed class CartLine
    {
        public required Guid ProductId { get; init; }
        public Guid? VariantId { get; init; }
        public required string Name { get; init; }
        public required decimal Price { get; init; }
        public required string Currency { get; init; }
        public int Qty { get; set; } = 1;
    }

    // ---- console state ----
    private WaiterTab _tab = WaiterTab.Floor;
    private HubConnection? _hub;
    private bool _connected;

    private WaiterDashboardDto? _widgets;
    private IReadOnlyList<TableOverviewRowDto> _floor = Array.Empty<TableOverviewRowDto>();
    private IReadOnlyList<ReadyToServeRowDto> _ready = Array.Empty<ReadyToServeRowDto>();
    private IReadOnlyList<CustomerRequestRowDto> _requests = Array.Empty<CustomerRequestRowDto>();
    private TableOverviewRowDto? _selectedFloorTable;
    private DerivedTableStatus? _statusFilter;
    private bool _mineOnly;
    private string? _myName;

    // ---- take-order state (existing flow) ----
    private IReadOnlyList<ProductDto>? _products;
    private IReadOnlyList<ProductCategoryDto> _categories = Array.Empty<ProductCategoryDto>();
    private IReadOnlyList<TableDto> _tables = Array.Empty<TableDto>();
    private PagedResult<OrderListItemDto>? _recent;

    private readonly List<CartLine> _cart = new();
    private OrderType _type = OrderType.DineIn;
    private TableDto? _selectedTable;
    private Guid? _currentSessionId;
    private string? _phone;
    private string? _name;
    private string? _notes;

    private string? _search;
    private Guid? _selectedCategoryId;
    private OrderListItemDto? _activeOrder;

    private bool _loading = true;
    private bool _placing;
    private string? _error;

    private string Currency => IsEditMode ? _activeOrder!.Currency
        : _cart.Count > 0 ? _cart[0].Currency : "Tk";
    private decimal Total => _cart.Sum(l => l.Price * l.Qty);
    private bool IsEditMode => _activeOrder is not null;
    private bool CanPlace => !_placing && _cart.Count > 0
        && (IsEditMode || _type != OrderType.DineIn || _selectedTable is not null);

    private IEnumerable<ProductCategoryDto> VisibleCategories =>
        _categories.Where(c => _products?.Any(p => p.ProductCategoryId == c.Id) == true);

    private IEnumerable<OrderListItemDto> RunningOrders =>
        _recent?.Items.Where(o => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled))
        ?? Enumerable.Empty<OrderListItemDto>();

    private IEnumerable<TableOverviewRowDto> FilteredFloor =>
        _floor.Where(t => (_statusFilter is null || t.Status == _statusFilter)
                          && (!_mineOnly || (t.WaiterName is { } w && w == _myName)));

    private IEnumerable<ProductDto> FilteredProducts =>
        (_products ?? Array.Empty<ProductDto>()).Where(p =>
            (_selectedCategoryId is null || p.ProductCategoryId == _selectedCategoryId)
            && (string.IsNullOrWhiteSpace(_search)
                || p.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)
                || p.Code.Contains(_search, StringComparison.OrdinalIgnoreCase)
                || (p.BanglaName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false)));

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthState.GetAuthenticationStateAsync();
        _myName = auth.User.Identity?.Name;

        _products = (await Sender.Send(new GetProductsQuery())).Where(p => p.IsActive).ToList();
        _categories = await Sender.Send(new GetProductCategoriesQuery());
        _tables = await Sender.Send(new GetTablesQuery());
        await LoadConsoleAsync();
        await LoadRecent();
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

            _hub.Reconnecting += _ => InvokeAsync(() => { _connected = false; StateHasChanged(); });
            _hub.Reconnected += _ => InvokeAsync(() => { _connected = true; StateHasChanged(); });
            _hub.Closed += _ => InvokeAsync(() => { _connected = false; StateHasChanged(); });

            _hub.On<string>(DashboardHub.ChangedEvent, _ =>
                InvokeAsync(async () =>
                {
                    await LoadConsoleAsync();
                    await LoadRecent();
                    StateHasChanged();
                }));

            await _hub.StartAsync();
            _connected = true;
            StateHasChanged();
        }
        catch
        {
            // Real-time is best-effort; the console still works via manual refresh.
        }
    }

    private async Task LoadConsoleAsync()
    {
        _widgets = await Sender.Send(new GetWaiterDashboardQuery());
        _floor = await Sender.Send(new GetTableOverviewQuery());
        _ready = await Sender.Send(new GetReadyToServeQuery());
        _requests = await Sender.Send(new GetCustomerRequestsQuery(CustomerRequestStatus.Pending));

        if (_selectedFloorTable is { } sel)
            _selectedFloorTable = _floor.FirstOrDefault(t => t.TableId == sel.TableId);
    }

    private async Task LoadRecent()
    {
        _recent = await Sender.Send(new GetOrdersQuery(Page: 1, PageSize: 50));

        if (_activeOrder is not null)
        {
            var fresh = _recent.Items.FirstOrDefault(o => o.Id == _activeOrder.Id);
            if (fresh is null || fresh.Status is OrderStatus.Completed or OrderStatus.Cancelled || fresh.IsPaid)
            {
                ToastService.ShowInfo($"Order {_activeOrder.OrderNumber} is no longer open — back to new order.");
                _activeOrder = null;
                _cart.Clear();
            }
            else
            {
                _activeOrder = fresh;
            }
        }

        StateHasChanged();
    }

    private async Task RefreshAllAsync()
    {
        await LoadConsoleAsync();
        await LoadRecent();
    }

    private async Task NotifyAsync(string scope) => await Notifier.NotifyAsync(scope);

    // ---- floor / table actions ----
    private void SelectFloorTable(TableOverviewRowDto t) =>
        _selectedFloorTable = _selectedFloorTable?.TableId == t.TableId ? null : t;

    private void SetTab(WaiterTab tab) => _tab = tab;
    private void SetStatusFilter(DerivedTableStatus? s) => _statusFilter = s;

    private async Task OpenTableAsync(TableOverviewRowDto t)
    {
        var res = await DialogService.ShowAsync<GuestCountDialog, GuestCountInput>(
            new GuestCountInput($"How many guests at table {t.TableNumber}?", 2),
            new BoDialogOptions { Title = $"Open table {t.TableNumber}", Width = "360px" });
        if (res.Cancelled || res.Data is not int guests) return;

        try
        {
            var opened = await Sender.Send(new OpenSessionCommand(t.TableId, guests));
            ToastService.ShowSuccess($"Table {t.TableNumber} opened · {opened.SessionNumber}");
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
            // Jump straight into taking the first order.
            StartTakeOrder(t, opened.SessionId, guests);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private void TakeOrderForTable(TableOverviewRowDto t) =>
        StartTakeOrder(t, t.SessionId, t.GuestCount ?? 0);

    private void StartTakeOrder(TableOverviewRowDto t, Guid? sessionId, int guests)
    {
        DeselectOrder();
        _type = OrderType.DineIn;
        _selectedTable = _tables.FirstOrDefault(x => x.Id == t.TableId);
        _currentSessionId = sessionId;
        _tab = WaiterTab.TakeOrder;
    }

    private async Task ChangeGuestsAsync(TableOverviewRowDto t)
    {
        if (t.SessionId is not { } sessionId) return;
        var res = await DialogService.ShowAsync<GuestCountDialog, GuestCountInput>(
            new GuestCountInput($"Guests at table {t.TableNumber}", t.GuestCount ?? 0),
            new BoDialogOptions { Title = "Change guest count", Width = "360px" });
        if (res.Cancelled || res.Data is not int guests) return;

        try
        {
            await Sender.Send(new ChangeSessionGuestCountCommand(sessionId, guests));
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private async Task MoveTableAsync(TableOverviewRowDto t)
    {
        if (t.SessionId is not { } sessionId) return;
        var free = FreeTableOptions(except: t.TableId);
        var res = await DialogService.ShowAsync<PickTableDialog, PickTableInput>(
            new PickTableInput($"Move table {t.TableNumber} to…", free),
            new BoDialogOptions { Title = "Move / transfer table", Width = "440px" });
        if (res.Cancelled || res.Data is not Guid target) return;

        try
        {
            await Sender.Send(new MoveSessionTableCommand(sessionId, target));
            ToastService.ShowSuccess("Table moved.");
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private async Task MergeTablesAsync(TableOverviewRowDto t)
    {
        if (t.SessionId is not { } survivorId) return;
        var candidates = _floor
            .Where(x => x.SessionId is not null && x.SessionId != survivorId)
            .Select(x => new MergeCandidate(x.SessionId!.Value, x.TableNumber, x.CurrentBill, x.Currency))
            .ToList();

        var res = await DialogService.ShowAsync<MergeTablesDialog, MergeInput>(
            new MergeInput(t.TableNumber, candidates),
            new BoDialogOptions { Title = "Merge tables", Width = "440px" });
        if (res.Cancelled || res.Data is not List<Guid> sources || sources.Count == 0) return;

        try
        {
            await Sender.Send(new MergeSessionsCommand(survivorId, sources));
            ToastService.ShowSuccess("Tables merged.");
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private async Task SplitTableAsync(TableOverviewRowDto t)
    {
        if (t.SessionId is not { } sessionId) return;
        var bill = await Sender.Send(new GetSessionBillQuery(sessionId));
        var orders = bill.Orders
            .Where(o => !o.IsPaid)
            .Select(o => new SplitOrderOption(o.OrderId, o.OrderNumber, o.Status, o.OrderTotal, bill.Currency))
            .ToList();
        var free = FreeTableOptions(except: t.TableId);

        var res = await DialogService.ShowAsync<SplitSessionDialog, SplitInput>(
            new SplitInput(orders, free),
            new BoDialogOptions { Title = $"Split table {t.TableNumber}", Width = "480px" });
        if (res.Cancelled || res.Data is not SplitResult split) return;

        try
        {
            var created = await Sender.Send(new SplitSessionCommand(sessionId, split.OrderIds, split.TargetTableId, split.Guests));
            ToastService.ShowSuccess($"Split to new session {created.SessionNumber}.");
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private async Task TransferWaiterAsync(TableOverviewRowDto t)
    {
        if (t.SessionId is not { } sessionId) return;
        var users = await Sender.Send(new GetUsersQuery());
        var staff = users
            .Where(u => u.IsActive && u.Roles.Any(r => r is Roles.Waiter or Roles.Manager or Roles.Admin or Roles.SuperAdmin))
            .Select(u => new StaffOption(u.Id, string.IsNullOrWhiteSpace(u.FullName) ? u.UserName : u.FullName, string.Join(", ", u.Roles)))
            .ToList();

        var res = await DialogService.ShowAsync<TransferWaiterDialog, TransferWaiterInput>(
            new TransferWaiterInput(t.WaiterName ?? "Unassigned", staff),
            new BoDialogOptions { Title = "Transfer waiter", Width = "440px" });
        if (res.Cancelled || res.Data is not TransferWaiterResult tr) return;

        try
        {
            await Sender.Send(new TransferSessionWaiterCommand(sessionId, tr.WaiterUserId, tr.WaiterName));
            ToastService.ShowSuccess("Waiter transferred.");
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private async Task ViewBillAsync(TableOverviewRowDto t)
    {
        if (t.SessionId is not { } sessionId) return;
        var res = await DialogService.ShowAsync<SessionBillDialog, Guid>(
            sessionId,
            new BoDialogOptions { Title = $"Bill · Table {t.TableNumber}", Width = "560px" });
        if (!res.Cancelled && res.Data is true)
        {
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
        }
    }

    private async Task RequestPaymentAsync(TableOverviewRowDto t)
    {
        if (t.SessionId is not { } sessionId) return;
        try
        {
            await Sender.Send(new RequestCashierSettlementCommand(sessionId));
            ToastService.ShowSuccess("Cashier settlement requested.");
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private async Task CloseSessionAsync(TableOverviewRowDto t)
    {
        if (t.SessionId is not { } sessionId) return;
        var ok = await DialogService.ConfirmAsync("Close session",
            $"Close the session at table {t.TableNumber}? All orders must be settled.", "Close", "Cancel", "danger");
        if (!ok) return;

        try
        {
            await Sender.Send(new CloseSessionCommand(sessionId, null));
            ToastService.ShowSuccess($"Table {t.TableNumber} closed.");
            _selectedFloorTable = null;
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Sessions);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private void PrintKot(TableOverviewRowDto t)
    {
        if (t.OrderId is { } id) OpenPdf($"/reports/order/{id}/kot.pdf");
    }

    private void PrintBill(TableOverviewRowDto t)
    {
        if (t.OrderId is { } id) OpenPdf($"/reports/order/{id}/receipt.pdf");
    }

    private void OpenPdf(string url) => Js.InvokeVoidAsync("open", url, "_blank");

    private List<TableOption> FreeTableOptions(Guid except)
    {
        var occupied = _floor.Where(f => f.SessionId is not null).Select(f => f.TableId).ToHashSet();
        return _tables
            .Where(t => t.Id != except && !occupied.Contains(t.Id))
            .Select(t => new TableOption(t.Id, t.TableNumber, t.Capacity))
            .ToList();
    }

    // ---- ready-to-serve ----
    private async Task ServeAsync(Guid orderId)
    {
        try
        {
            await Sender.Send(new ChangeOrderStatusCommand(orderId, OrderStatus.Served));
            ToastService.ShowSuccess("Order served.");
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Orders);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private async Task ServeAllAsync()
    {
        var ids = _ready.Select(r => r.OrderId).ToList();
        foreach (var id in ids)
        {
            try { await Sender.Send(new ChangeOrderStatusCommand(id, OrderStatus.Served)); }
            catch { /* skip ones that changed underneath */ }
        }
        ToastService.ShowSuccess("Ready orders served.");
        await RefreshAllAsync();
        await NotifyAsync(DashboardScopes.Orders);
    }

    // ---- requests ----
    private async Task ResolveRequestAsync(Guid id)
    {
        try
        {
            await Sender.Send(new ResolveCustomerRequestCommand(id));
            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.Requests);
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    // ---- take-order (existing flow) ----
    private void SelectCategory(Guid? categoryId) => _selectedCategoryId = categoryId;

    private void SetType(OrderType type)
    {
        _type = type;
        if (type == OrderType.Takeaway) { _selectedTable = null; _currentSessionId = null; }
    }

    private async Task SelectRunningOrderAsync(OrderListItemDto order)
    {
        _error = null;
        if (_activeOrder?.Id == order.Id) { DeselectOrder(); return; }

        try
        {
            var detail = await Sender.Send(new GetOrderQuery(order.Id));
            var hadDraft = _cart.Count > 0 && _activeOrder is null;
            _cart.Clear();
            foreach (var l in detail.Lines)
            {
                _cart.Add(new CartLine
                {
                    ProductId = l.MenuItemId,
                    VariantId = l.VariantId,
                    Name = l.Name,
                    Price = l.UnitPrice,
                    Currency = detail.Currency,
                    Qty = l.Quantity
                });
            }
            _activeOrder = order;
            _tab = WaiterTab.TakeOrder;
            if (hadDraft) ToastService.ShowInfo($"Draft cart replaced with the items of {order.OrderNumber}.");
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }

    private void DeselectOrder()
    {
        _error = null;
        _activeOrder = null;
        _cart.Clear();
    }

    private int QtyOf(Guid productId) => _cart.Where(l => l.ProductId == productId).Sum(l => l.Qty);

    private void AddBase(ProductDto item)
    {
        if (item.HasVariants) return;
        AddToCart(item.Id, null, item.Name, item.Price, item.Currency);
    }

    private void AddVariant(ProductDto item, ProductVariantDto variant) =>
        AddToCart(item.Id, variant.Id, $"{item.Name} ({variant.Name})", variant.Price, item.Currency);

    private void AddToCart(Guid productId, Guid? variantId, string name, decimal price, string currency)
    {
        var existing = _cart.FirstOrDefault(l => l.ProductId == productId && l.VariantId == variantId);
        if (existing is null)
            _cart.Add(new CartLine { ProductId = productId, VariantId = variantId, Name = name, Price = price, Currency = currency });
        else
            existing.Qty++;
    }

    private void Increment(CartLine line) => line.Qty++;

    private void Decrement(CartLine line)
    {
        if (line.Qty <= 1) _cart.Remove(line);
        else line.Qty--;
    }

    private void Remove(CartLine line) => _cart.Remove(line);
    private void ClearCart() => _cart.Clear();

    private async Task PlaceAsync()
    {
        _error = null;
        _placing = true;
        try
        {
            var lines = _cart.Select(l => new PlaceOrderLineInput(l.ProductId, l.Qty, l.VariantId)).ToList();

            if (IsEditMode)
            {
                var result = await Sender.Send(new UpdateWaiterOrderLinesCommand(_activeOrder!.Id, lines));
                ToastService.ShowSuccess($"Order {result.OrderNumber} updated · {result.Currency} {result.Total:0.##}");
                _cart.Clear();
                _activeOrder = null;
            }
            else
            {
                var result = await Sender.Send(new PlaceWaiterOrderCommand(
                    _phone, _name, _selectedTable?.Id, _type, _notes, lines, null, _currentSessionId));
                ToastService.ShowSuccess($"Order {result.OrderNumber} placed · {result.Currency} {result.Total:0.##}");
                _cart.Clear();
                _phone = _name = _notes = null;
                _selectedTable = null;
                _currentSessionId = null;
            }

            await RefreshAllAsync();
            await NotifyAsync(DashboardScopes.All);
        }
        catch (FluentValidation.ValidationException vex)
        {
            _error = vex.Errors.Any() ? string.Join("; ", vex.Errors.Select(e => e.ErrorMessage)) : vex.Message;
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _placing = false; }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            try { await _hub.DisposeAsync(); } catch { /* ignore */ }
        }
    }

    // ---- view helpers ----
    private static string? BanglaOf(ProductCategoryDto cat)
    {
        var bn = cat.Description?.Split('—')[0].Trim();
        return string.IsNullOrEmpty(bn) ? null : bn;
    }

    private static string ChipTitle(OrderListItemDto o) =>
        $"{(o.OrderType == OrderType.DineIn ? o.TableNumber ?? "T?" : "TW")}#{ShortNo(o.OrderNumber)}";

    private static string ShortNo(string orderNumber) => orderNumber[(orderNumber.LastIndexOf('-') + 1)..];

    private static string StatusTone(OrderStatus status) => status switch
    {
        OrderStatus.Placed => "warning",
        OrderStatus.Confirmed => "info",
        OrderStatus.Preparing => "primary",
        OrderStatus.Ready => "success",
        OrderStatus.Served => "success",
        OrderStatus.Cancelled => "danger",
        _ => "neutral"
    };

    private static string TableTone(DerivedTableStatus s) => s switch
    {
        DerivedTableStatus.Available => "success",
        DerivedTableStatus.Occupied => "warning",
        DerivedTableStatus.Reserved => "info",
        DerivedTableStatus.WaitingPayment => "danger",
        _ => "neutral"
    };

    private static string TableStatusLabel(DerivedTableStatus s) => s switch
    {
        DerivedTableStatus.Available => "Available",
        DerivedTableStatus.Occupied => "Occupied",
        DerivedTableStatus.Reserved => "Reserved",
        DerivedTableStatus.WaitingPayment => "Waiting payment",
        _ => s.ToString()
    };

    private static string RequestLabel(CustomerRequestType t) => t switch
    {
        CustomerRequestType.CallWaiter => "Call Waiter",
        CustomerRequestType.RequestBill => "Request Bill",
        CustomerRequestType.NeedWater => "Need Water",
        CustomerRequestType.NeedTissue => "Need Tissue",
        _ => t.ToString()
    };
}
