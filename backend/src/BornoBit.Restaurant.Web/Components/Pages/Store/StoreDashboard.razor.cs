using BornoBit.Restaurant.Application.Store.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreDashboard : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private StoreDashboardSummaryDto? _summary;
    private List<StoreLowStockRow> _lowStock = new();

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _summary = await Mediator.Send(new GetStoreDashboardSummaryQuery());
            _lowStock = (await Mediator.Send(new GetStoreLowStockRowsQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load dashboard: {ex.Message}"; }
        finally { _loading = false; }
    }

    private string Money(decimal v) => $"{v:#,##0.00} {_summary?.Currency ?? "Tk"}";
}
