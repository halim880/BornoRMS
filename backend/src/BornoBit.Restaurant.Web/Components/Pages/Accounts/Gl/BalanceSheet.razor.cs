using BornoBit.Restaurant.Application.Accounting.Reports;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts.Gl;

public partial class BalanceSheet : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private Task PrintAsync() => Js.InvokeVoidAsync("window.print").AsTask();

    private bool _loading = true;
    private string? _error;
    private BalanceSheetDto _report = new(
        Array.Empty<GlAccountLineDto>(), 0m, Array.Empty<GlAccountLineDto>(), 0m,
        Array.Empty<GlAccountLineDto>(), 0m, 0m, true);

    private DateTime _asOf = DateTime.UtcNow.Date;

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _report = await Mediator.Send(new GetBalanceSheetQuery(_asOf));
        }
        catch (Exception ex) { _error = $"Failed to load balance sheet: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnAsOfChanged(DateTime? d) { if (d.HasValue) _asOf = d.Value.Date; return ReloadAsync(); }
}
