using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BornoBit.Restaurant.Web.Components.Pages.Operations.Reports;

public partial class SalesInvoiceReport : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private Task PrintAsync() => Js.InvokeVoidAsync("window.print").AsTask();

    private bool _loading = true;
    private string? _error;
    private string? _rangeError;

    private SalesInvoiceReportDto _report = new(Array.Empty<SalesInvoiceRowDto>(), 0, 0m, 0m, 0m, "Tk");

    private DateTime _from = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    private DateTime _to = DateTime.UtcNow.Date;

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _rangeError = null;
        if (_from > _to)
        {
            _rangeError = "The From date is after the To date.";
            _report = new SalesInvoiceReportDto(Array.Empty<SalesInvoiceRowDto>(), 0, 0m, 0m, 0m, "Tk");
            return;
        }

        _loading = true; _error = null;
        try
        {
            _report = await Mediator.Send(new GetSalesInvoiceReportQuery(_from, _to));
        }
        catch (Exception ex) { _error = $"Failed to load sales report: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnFromChanged(DateTime? d) { if (d.HasValue) _from = d.Value.Date; return ReloadAsync(); }
    private Task OnToChanged(DateTime? d) { if (d.HasValue) _to = d.Value.Date; return ReloadAsync(); }

    private string PdfUrl()
    {
        var from = DateTime.SpecifyKind(_from.Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(_to.Date, DateTimeKind.Utc);
        return $"/reports/sales/invoices.pdf?fromUtc={from:o}&toUtc={to:o}";
    }

    private static string OrderTypeTone(OrderType type) => type switch
    {
        OrderType.DineIn => "primary",
        OrderType.Takeaway => "info",
        OrderType.Delivery => "warning",
        _ => "neutral",
    };

    private static string PaymentTone(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "success",
        PaymentMethod.Card => "info",
        PaymentMethod.Mobile => "primary",
        _ => "neutral",
    };
}
