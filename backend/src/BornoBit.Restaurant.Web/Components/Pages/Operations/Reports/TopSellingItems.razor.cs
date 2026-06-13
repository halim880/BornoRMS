using BornoBit.Restaurant.Application.Ordering.Queries;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Operations.Reports;

public partial class TopSellingItems : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private Task PrintAsync() => Js.InvokeVoidAsync("window.print").AsTask();

    private bool _loading = true;
    private string? _error;
    private string? _rangeError;

    private List<TopItemRowDto> _rows = new();

    private DateTime _from = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    private DateTime _to = DateTime.UtcNow.Date;
    private int _top = 20;

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _rangeError = null;
        if (_from > _to)
        {
            _rangeError = "The From date is after the To date.";
            _rows = new();
            return;
        }

        _loading = true; _error = null;
        try
        {
            _rows = (await Mediator.Send(new GetTopSellingItemsQuery(_from, _to, _top))).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load top items: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnFromChanged(DateTime? d) { if (d.HasValue) _from = d.Value.Date; return ReloadAsync(); }
    private Task OnToChanged(DateTime? d) { if (d.HasValue) _to = d.Value.Date; return ReloadAsync(); }
    private Task OnTopChanged(int n) { _top = n < 1 ? 1 : n; return ReloadAsync(); }
}
