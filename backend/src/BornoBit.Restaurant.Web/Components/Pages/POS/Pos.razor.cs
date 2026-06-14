using BornoBit.Restaurant.Application.Dining.Queries;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Application.Ordering.Pos;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Application.ProductCategories;
using BornoBit.Restaurant.Application.Products;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using BornoBit.Restaurant.Web.Components.Pages.Operations.Dialogs;
using BornoBit.Restaurant.Web.Components.Shared;
using BornoBit.Restaurant.Web.Hubs;
using BornoBit.Restaurant.Web.Services.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.POS;

public partial class Pos : ComponentBase, IAsyncDisposable
{
    [Inject] private ISender Sender { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private IDashboardNotifier Notifier { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private IReadOnlyList<ProductDto> _products = Array.Empty<ProductDto>();
    private IReadOnlyList<ProductCategoryDto> _categories = Array.Empty<ProductCategoryDto>();
    private IReadOnlyList<TableDto> _tables = Array.Empty<TableDto>();
    private Dictionary<Guid, ProductAvailabilityDto> _availability = new();
    private List<ActiveOrderDto> _activeOrders = new();

    private Guid? _activeOrderId;
    private OrderDetailDto? _activeDetail;
    private Guid? _selectedCategoryId;

    private bool _loading = true;
    private bool _busy;

    private ElementReference _chipsEl;
    private bool _chipsObserverWired;
    private bool _chipsOverflowing;
    private DotNetObjectReference<Pos>? _selfRef;

    // Real-time: reuse the shared DashboardHub tick channel (no POS-specific hub needed).
    private HubConnection? _hub;
    private bool _connected;

    // Categories already sorted by DisplayOrder; only show ones with active products.
    private IEnumerable<ProductCategoryDto> VisibleCategories =>
        _categories.Where(c => _products.Any(p => p.ProductCategoryId == c.Id));

    private IEnumerable<ProductDto> FilteredProducts =>
        _products.Where(p => _selectedCategoryId is null || p.ProductCategoryId == _selectedCategoryId);

    protected override async Task OnInitializedAsync()
    {
        _products = (await Sender.Send(new GetProductsQuery()))
            .Where(p => p.IsActive)
            .ToList();
        _categories = await Sender.Send(new GetProductCategoriesQuery());
        _tables = await Sender.Send(new GetTablesQuery());
        await LoadAvailabilityAsync();
        await LoadActiveOrdersAsync();
        _loading = false;
    }

    private async Task LoadAvailabilityAsync() =>
        _availability = (await Sender.Send(new GetProductAvailabilityQuery())).ToDictionary(a => a.ProductId);

    // ----- stock availability helpers (DirectStock products only; others are unconstrained) -----
    private bool IsOutOfStock(ProductDto p) => _availability.TryGetValue(p.Id, out var a) && a.IsOutOfStock;
    private bool IsLowStock(ProductDto p) => _availability.TryGetValue(p.Id, out var a) && a.IsLowStock && !a.IsOutOfStock;
    private decimal? AvailableOf(ProductDto p) => _availability.TryGetValue(p.Id, out var a) ? a.AvailableStock : null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await ConnectRealtimeAsync();

        // The chips strip only exists once loading finished, so wire the observer on the
        // first render that actually has it, not strictly on firstRender.
        if (!_chipsObserverWired && !_loading)
        {
            _chipsObserverWired = true;
            _selfRef = DotNetObjectReference.Create(this);
            await Js.InvokeVoidAsync("posChips.observe", _chipsEl, _selfRef);
        }
    }

    /// <summary>
    /// Subscribes to the DashboardHub "changed" tick so order chips / occupancy stay in sync across
    /// terminals within the poll window (≤2s when handlers push a direct notify). Best-effort: the POS
    /// keeps working via manual reload if the hub never connects.
    /// </summary>
    private async Task ConnectRealtimeAsync()
    {
        try
        {
            _hub = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/hubs/dashboard"))
                .WithAutomaticReconnect()
                .Build();

            _hub.Reconnecting += _ => InvokeAsync(() => { _connected = false; StateHasChanged(); });
            _hub.Reconnected += _ => InvokeAsync(() => { _connected = true; StateHasChanged(); });
            _hub.Closed += _ => InvokeAsync(() => { _connected = false; StateHasChanged(); });

            _hub.On<string>(DashboardHub.ChangedEvent, _ => InvokeAsync(OnRealtimeTickAsync));

            await _hub.StartAsync();
            _connected = true;
            StateHasChanged();
        }
        catch
        {
            // Real-time is best-effort; the terminal still works via manual reload.
        }
    }

    /// <summary>
    /// A peer terminal changed something. Re-pull the active queue and the open detail, but never while
    /// this terminal is mid-save — the in-flight write is the newer truth and will re-sync on completion.
    /// </summary>
    private async Task OnRealtimeTickAsync()
    {
        if (_busy) return;
        await LoadAvailabilityAsync();
        await LoadActiveOrdersAsync();
        if (_activeOrderId is { } id && _activeOrders.Any(o => o.Id == id))
            await RefreshActiveDetailAsync(id);
        StateHasChanged();
    }

    /// <summary>Fan a tick out so other terminals refresh after this one mutates an order.</summary>
    private Task NotifyPeersAsync(string scope = DashboardScopes.Orders) => Notifier.NotifyAsync(scope);

    [JSInvokable]
    public Task OnChipsOverflowChanged(bool overflowing)
    {
        if (_chipsOverflowing == overflowing) return Task.CompletedTask;
        _chipsOverflowing = overflowing;
        return InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_chipsObserverWired)
                await Js.InvokeVoidAsync("posChips.dispose", _chipsEl);
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
        _selfRef?.Dispose();

        if (_hub is not null)
        {
            try { await _hub.DisposeAsync(); } catch { /* ignore */ }
        }
    }

    // ----- chips -----

    private async Task LoadActiveOrdersAsync()
    {
        _activeOrders = (await Sender.Send(new GetActiveOrdersQuery())).ToList();

        // Drop the selection if the order was paid/cancelled elsewhere meanwhile.
        if (_activeOrderId is { } id && _activeOrders.All(o => o.Id != id))
        {
            _activeOrderId = null;
            _activeDetail = null;
        }
    }

    private async Task SelectOrderAsync(Guid orderId)
    {
        if (_busy) return;
        try
        {
            _activeDetail = await Sender.Send(new GetOrderQuery(orderId));
            _activeOrderId = orderId;
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
    }

    /// <summary>T-{table digits} for dine-in; W-/D-/C- + daily sequence for the rest; TK- for legacy takeaway.</summary>
    internal static string ChipLabel(ActiveOrderDto o)
    {
        if (o.OrderType == OrderType.DineIn)
        {
            var digits = new string((o.TableNumber ?? "").Where(char.IsDigit).ToArray());
            return digits.Length > 0 ? $"T-{digits.PadLeft(2, '0')}" : $"T-{o.TableNumber ?? "?"}";
        }

        var prefix = o.OrderType switch
        {
            OrderType.Waiting => "W",
            OrderType.Delivery => "D",
            OrderType.Collection => "C",
            _ => "TK"
        };

        // "ORD-yyyyMMdd-NNNN" → NNNN as a short zero-padded sequence.
        var seq = o.OrderNumber[(o.OrderNumber.LastIndexOf('-') + 1)..];
        var nn = int.TryParse(seq, out var n) ? n.ToString("D2") : seq;
        return $"{prefix}-{nn}";
    }

    private static string CustomerLabel(OrderDetailDto order)
    {
        if (!string.IsNullOrWhiteSpace(order.CustomerName)) return order.CustomerName;
        return order.CustomerPhone == Customer.WalkInPhone ? "Walk-in customer" : order.CustomerPhone;
    }

    private static string TypeLabel(OrderType type) => type switch
    {
        OrderType.DineIn => "Dine In",
        OrderType.Takeaway => "Takeaway",
        OrderType.Delivery => "Delivery",
        OrderType.Collection => "Collection",
        OrderType.Waiting => "Waiting",
        _ => type.ToString()
    };

    /// <summary>Tables held by open dine-in orders, optionally ignoring the order being edited.</summary>
    private IReadOnlySet<Guid> OccupiedTableIds(Guid? excludeOrderId = null) =>
        _activeOrders
            .Where(o => o.OrderType == OrderType.DineIn && o.TableId is not null && o.Id != excludeOrderId)
            .Select(o => o.TableId!.Value)
            .ToHashSet();

    // ----- cart mutations (every change is persisted; chip switching can never lose data) -----

    private async Task OnProductClickAsync(ProductDto item)
    {
        if (IsOutOfStock(item))
        {
            ToastService.ShowWarning($"{item.Name} is out of stock.");
            return;
        }

        if (_activeDetail is null)
        {
            ToastService.ShowInfo("Select an order chip first, or press + to start a new order.");
            return;
        }

        if (!item.HasVariants)
        {
            await AddProductAsync(item.Id, null);
            return;
        }

        var result = await DialogService.ShowAsync<VariantPickerDialog, ProductDto>(item, new BoDialogOptions
        {
            Title = item.Name,
            Width = "360px"
        });
        if (!result.Cancelled && result.Data is ProductVariantDto variant)
            await AddProductAsync(item.Id, variant.Id);
    }

    private async Task AddProductAsync(Guid productId, Guid? variantId)
    {
        if (_activeDetail is null) return;

        var desired = ToLineInputs(_activeDetail.Lines);
        var existing = desired.FindIndex(l => l.MenuItemId == productId && l.VariantId == variantId);
        if (existing >= 0)
            desired[existing] = desired[existing] with { Quantity = desired[existing].Quantity + 1 };
        else
            desired.Add(new PlaceOrderLineInput(productId, 1, variantId));

        await SaveLinesAsync(desired);
    }

    private async Task ChangeQtyAsync(OrderLineDto line, int delta)
    {
        if (_activeDetail is null) return;

        var desired = ToLineInputs(_activeDetail.Lines);
        var idx = desired.FindIndex(l => l.MenuItemId == line.MenuItemId && l.VariantId == line.VariantId);
        if (idx < 0) return;

        var qty = desired[idx].Quantity + delta;
        if (qty <= 0) desired.RemoveAt(idx);
        else desired[idx] = desired[idx] with { Quantity = qty };

        await SaveLinesAsync(desired);
    }

    private async Task RemoveLineAsync(OrderLineDto line)
    {
        if (_activeDetail is null) return;

        var desired = ToLineInputs(_activeDetail.Lines)
            .Where(l => !(l.MenuItemId == line.MenuItemId && l.VariantId == line.VariantId))
            .ToList();

        await SaveLinesAsync(desired);
    }

    private static List<PlaceOrderLineInput> ToLineInputs(IReadOnlyList<OrderLineDto> lines) =>
        lines.Select(l => new PlaceOrderLineInput(l.MenuItemId, l.Quantity, l.VariantId)).ToList();

    private async Task SaveLinesAsync(List<PlaceOrderLineInput> desired)
    {
        if (_activeOrderId is not { } orderId || _busy) return;

        _busy = true;
        try
        {
            await Sender.Send(new SetPosOrderLinesCommand(orderId, desired));
            await RefreshActiveDetailAsync(orderId);
            await NotifyPeersAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
            await LoadActiveOrdersAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RefreshActiveDetailAsync(Guid orderId)
    {
        _activeDetail = await Sender.Send(new GetOrderQuery(orderId));

        var idx = _activeOrders.FindIndex(o => o.Id == orderId);
        if (idx >= 0)
        {
            _activeOrders[idx] = _activeOrders[idx] with
            {
                ItemCount = _activeDetail.Lines.Count,
                Total = _activeDetail.GrandTotal
            };
        }
    }

    // ----- dialogs / actions -----

    private async Task OpenNewOrderDialogAsync()
    {
        var model = new NewOrderModel
        {
            Tables = _tables,
            OccupiedTableIds = OccupiedTableIds()
        };
        var result = await DialogService.ShowAsync<NewOrderDialog, NewOrderModel>(model, new BoDialogOptions
        {
            Title = "New order",
            Width = "440px",
            DismissOnOverlayClick = false
        });

        if (!result.Cancelled && result.Data is NewOrderModel { CreatedOrderId: { } createdId } saved)
        {
            ToastService.ShowSuccess($"Order {saved.CreatedOrderNumber} started.");
            await LoadActiveOrdersAsync();
            await SelectOrderAsync(createdId);
            await NotifyPeersAsync(DashboardScopes.All);
        }
    }

    private async Task OpenEditOrderDialogAsync()
    {
        if (_activeDetail is null) return;

        var model = new NewOrderModel
        {
            Type = _activeDetail.OrderType,
            TableId = _tables.FirstOrDefault(t => t.TableNumber == _activeDetail.TableNumber)?.Id,
            Name = _activeDetail.CustomerName,
            Phone = _activeDetail.CustomerPhone == Customer.WalkInPhone ? null : _activeDetail.CustomerPhone,
            Address = _activeDetail.CustomerAddress,
            Tables = _tables,
            OccupiedTableIds = OccupiedTableIds(excludeOrderId: _activeDetail.Id),
            EditOrderId = _activeDetail.Id,
            EditOrderNumber = _activeDetail.OrderNumber
        };
        var result = await DialogService.ShowAsync<NewOrderDialog, NewOrderModel>(model, new BoDialogOptions
        {
            Title = $"Edit order · {_activeDetail.OrderNumber}",
            Width = "440px",
            DismissOnOverlayClick = false
        });

        if (!result.Cancelled && result.Data is NewOrderModel)
        {
            ToastService.ShowSuccess($"Order {_activeDetail.OrderNumber} updated.");
            await LoadActiveOrdersAsync();
            if (_activeOrderId is { } id)
                await RefreshActiveDetailAsync(id);
            await NotifyPeersAsync(DashboardScopes.All);
        }
    }

    /// <summary>Open/close the cashier's cash drawer (shift) — the float a POS terminal settles cash into.</summary>
    private async Task OpenDrawerDialogAsync()
    {
        await DialogService.ShowAsync<DrawerDialog, bool>(true, new BoDialogOptions
        {
            Title = "Cash drawer",
            Width = "420px"
        });
    }

    private async Task OpenRunningOrdersAsync()
    {
        var model = new RunningOrdersModel { Orders = _activeOrders, ActiveOrderId = _activeOrderId };
        var result = await DialogService.ShowAsync<RunningOrdersDialog, RunningOrdersModel>(model, new BoDialogOptions
        {
            Title = "Running orders",
            Width = "480px"
        });

        if (!result.Cancelled && result.Data is Guid orderId)
            await SelectOrderAsync(orderId);
    }

    private async Task OpenPaymentDialogAsync()
    {
        if (_activeOrderId is not { } orderId) return;

        var result = await DialogService.ShowAsync<PaymentDialog, Guid>(orderId, new BoDialogOptions
        {
            Title = "Payment",
            Width = "400px",
            DismissOnOverlayClick = false
        });

        // Paid orders drop out of the active queue; rounding tweaks without payment changed totals.
        await LoadActiveOrdersAsync();
        if (result.Data is true)
        {
            _activeOrderId = null;
            _activeDetail = null;
        }
        else if (_activeOrderId is { } stillActive)
        {
            await RefreshActiveDetailAsync(stillActive);
        }
        await NotifyPeersAsync(DashboardScopes.All);
    }

    /// <summary>View an order's payment ledger and void/refund tenders (manager-gated, instant override).</summary>
    private async Task OpenPaymentsDialogAsync()
    {
        if (_activeOrderId is not { } orderId) return;

        var result = await DialogService.ShowAsync<PaymentsDialog, Guid>(orderId, new BoDialogOptions
        {
            Title = "Payments",
            Width = "440px"
        });

        if (result.Data is true)
        {
            await LoadActiveOrdersAsync();
            if (_activeOrderId is { } id && _activeOrders.Any(o => o.Id == id))
                await RefreshActiveDetailAsync(id);
            else { _activeOrderId = null; _activeDetail = null; }
            await NotifyPeersAsync(DashboardScopes.Payments);
        }
    }

    private async Task CancelActiveOrderAsync()
    {
        if (_activeDetail is null) return;

        var result = await DialogService.ShowAsync<CancelOrderDialog, string>(_activeDetail.OrderNumber, new BoDialogOptions
        {
            Title = "Cancel order",
            Width = "420px"
        });
        if (result.Cancelled) return;

        try
        {
            await Sender.Send(new ChangeOrderStatusCommand(_activeDetail.Id, OrderStatus.Cancelled, result.Data as string));
            ToastService.ShowSuccess($"Order {_activeDetail.OrderNumber} cancelled.");
            _activeOrderId = null;
            _activeDetail = null;
            await LoadActiveOrdersAsync();
            await NotifyPeersAsync(DashboardScopes.All);
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
    }
}
