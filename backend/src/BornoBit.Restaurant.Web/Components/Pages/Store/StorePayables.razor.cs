using BornoBit.Restaurant.Application.Store.Payments;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StorePayables : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<StoreSupplierPayableDto> _rows = new();
    private List<StorePaymentDto> _payments = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _rows = (await Mediator.Send(new GetStoreSupplierPayablesQuery())).ToList();
            _payments = (await Mediator.Send(new GetStorePaymentsQuery(Take: 50))).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load payables: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowPayAsync(StoreSupplierPayableDto s)
    {
        var model = new StorePayModel
        {
            SupplierId = s.SupplierId,
            SupplierName = s.Name,
            Outstanding = s.Outstanding,
            Amount = s.Outstanding > 0 ? s.Outstanding : 0m
        };
        var result = await DialogService.ShowAsync<StorePayDialog, StorePayModel>(model, new BoDialogOptions
        {
            Title = $"Record payment · {s.Name}",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StorePayModel saved && saved.Saved)
        {
            ToastService.ShowSuccess($"Payment recorded for '{s.Name}'.");
            await ReloadAsync();
        }
    }
}
