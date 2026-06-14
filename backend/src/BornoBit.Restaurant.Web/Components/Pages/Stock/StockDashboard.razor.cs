using BornoBit.Restaurant.Application.Inventory.Dashboard;
using BornoBit.Restaurant.Web.Hubs;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class StockDashboard : ComponentBase, IAsyncDisposable
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _loading = true;
    private string? _error;

    private StockValuationDto? _valuation;
    private FastSlowMoversDto? _movers;
    private WastePercentDto? _waste;
    private List<OutOfStockRow> _outOfStock = new();
    private List<IngredientConsumptionRow> _consumption = new();

    private HubConnection? _hub;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _valuation = await Mediator.Send(new GetStockValuationQuery());
            _movers = await Mediator.Send(new GetFastSlowMoversQuery());
            _waste = await Mediator.Send(new GetWastePercentQuery());
            _outOfStock = (await Mediator.Send(new GetOutOfStockQuery())).ToList();
            _consumption = (await Mediator.Send(new GetIngredientConsumptionQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load dashboard: {ex.Message}"; }
        finally { _loading = false; }
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
                    await LoadAsync();
                    StateHasChanged();
                }));

            await _hub.StartAsync();
        }
        catch { /* live updates are best-effort; the page still works without the hub */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null) await _hub.DisposeAsync();
    }
}
