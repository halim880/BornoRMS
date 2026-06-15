using BornoBit.Restaurant.Application.Accounting.Reports;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts.Gl;

public partial class ProfitAndLossGl : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private Task PrintAsync() => Js.InvokeVoidAsync("window.print").AsTask();

    private bool _loading = true;
    private string? _error;
    private GlProfitAndLossDto _report = new(Array.Empty<GlAccountLineDto>(), 0m, Array.Empty<GlAccountLineDto>(), 0m, 0m);

    private DateTime _from = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    private DateTime _to = DateTime.UtcNow.Date;

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _report = await Mediator.Send(new GetGlProfitAndLossQuery(_from, _to));
        }
        catch (Exception ex) { _error = $"Failed to load P&L: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnFromChanged(DateTime? d) { if (d.HasValue) _from = d.Value.Date; return ReloadAsync(); }
    private Task OnToChanged(DateTime? d) { if (d.HasValue) _to = d.Value.Date; return ReloadAsync(); }
}
