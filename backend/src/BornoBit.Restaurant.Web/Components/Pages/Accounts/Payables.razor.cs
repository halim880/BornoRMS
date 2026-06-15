using BornoBit.Restaurant.Application.Inventory.Payables;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public partial class Payables : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;

    private List<PayableDto> _payables = new();
    private List<SupplierPaymentDto> _recent = new();
    private bool _outstandingOnly = true;

    private decimal TotalOutstanding => _payables.Sum(p => p.Outstanding);

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _payables = (await Mediator.Send(new GetPayablesQuery(_outstandingOnly))).ToList();
            _recent = (await Mediator.Send(new GetSupplierPaymentsQuery())).Take(20).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load payables: {ex.Message}"; }
        finally { _loading = false; }
    }

    private Task OnOutstandingOnlyChanged(bool v) { _outstandingOnly = v; return ReloadAsync(); }

    private async Task PayAsync(PayableDto p)
    {
        var model = new SupplierPaymentFormModel
        {
            SupplierId = p.SupplierId,
            SupplierName = p.SupplierName,
            Outstanding = p.Outstanding,
            Amount = p.Outstanding > 0m ? p.Outstanding : 0m,
            PaidOn = DateTime.UtcNow.Date
        };
        var result = await DialogService.ShowAsync<SupplierPaymentDialog, SupplierPaymentFormModel>(model, new BoDialogOptions
        {
            Title = $"Pay · {p.SupplierName}",
            Width = "520px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is SupplierPaymentFormModel saved && saved.Saved)
        {
            ToastService.ShowSuccess($"Payment to {p.SupplierName} recorded.");
            await ReloadAsync();
        }
    }
}
