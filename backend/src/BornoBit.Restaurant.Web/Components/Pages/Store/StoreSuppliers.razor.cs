using BornoBit.Restaurant.Application.Store.Suppliers;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreSuppliers : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<StoreSupplierDto> _suppliers = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _suppliers = (await Mediator.Send(new GetStoreSuppliersQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load suppliers: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var model = new StoreSupplierFormModel();
        var result = await DialogService.ShowAsync<StoreSupplierFormDialog, StoreSupplierFormModel>(model, new BoDialogOptions
        {
            Title = "New store supplier",
            Width = "520px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreSupplierFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Supplier '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(StoreSupplierDto s)
    {
        var model = new StoreSupplierFormModel
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            Phone = s.Phone,
            Address = s.Address,
            PaymentTermsDays = s.PaymentTermsDays,
            Notes = s.Notes
        };
        var result = await DialogService.ShowAsync<StoreSupplierFormDialog, StoreSupplierFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit · {s.Name}",
            Width = "520px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreSupplierFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Supplier updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(StoreSupplierDto s, bool active)
    {
        try
        {
            await Mediator.Send(new SetStoreSupplierActiveCommand(s.Id, active));
            ToastService.ShowSuccess($"Supplier '{s.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
