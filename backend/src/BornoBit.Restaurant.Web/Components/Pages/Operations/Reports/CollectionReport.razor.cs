using BornoBit.Restaurant.Application.Ordering.Queries;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Operations.Reports;

public partial class CollectionReport : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private Task PrintAsync() => Js.InvokeVoidAsync("window.print").AsTask();

    private bool _loading = true;
    private string? _error;
    private string? _rangeError;

    private CollectionReportDto _report = new(Array.Empty<CollectionMethodLineDto>(), 0, 0m, "Tk");

    private DateTime _from = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    private DateTime _to = DateTime.UtcNow.Date;

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _rangeError = null;
        if (_from > _to)
        {
            _rangeError = "The From date is after the To date.";
            _report = new CollectionReportDto(Array.Empty<CollectionMethodLineDto>(), 0, 0m, "Tk");
            return;
        }

        _loading = true; _error = null;
        try
        {
            _report = await Mediator.Send(new GetCollectionReportQuery(_from, _to));
        }
        catch (Exception ex) { _error = $"Failed to load collection report: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnFromChanged(DateTime? d) { if (d.HasValue) _from = d.Value.Date; return ReloadAsync(); }
    private Task OnToChanged(DateTime? d) { if (d.HasValue) _to = d.Value.Date; return ReloadAsync(); }
}
