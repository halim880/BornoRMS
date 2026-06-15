using BornoBit.Restaurant.Application.Accounting.Drawers;
using BornoBit.Restaurant.Application.Accounting.Reports;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts.Reports;

public partial class DayEnd : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private Task PrintAsync() => Js.InvokeVoidAsync("window.print").AsTask();

    private bool _loading = true;
    private string? _error;

    private DayEndReportDto _report = Empty(DateOnly.FromDateTime(DateTime.UtcNow));

    private DateTime _date = DateTime.UtcNow.Date;

    private static DayEndReportDto Empty(DateOnly date) => new(
        date, "Tk", 0, 0m, 0m, 0m,
        Array.Empty<DrawerMethodLineDto>(), 0m,
        Array.Empty<DrawerDto>(), 0m,
        Array.Empty<PlLineDto>(), 0m, 0, 0m);

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _report = await Mediator.Send(new GetDayEndReportQuery(DateOnly.FromDateTime(_date)));
        }
        catch (Exception ex) { _error = $"Failed to load day-end report: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnDateChanged(DateTime? d) { if (d.HasValue) _date = d.Value.Date; return ReloadAsync(); }
}
