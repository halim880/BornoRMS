using BornoBit.Restaurant.Application.Store.Departments;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreDepartments : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<StoreDepartmentDto> _departments = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _departments = (await Mediator.Send(new GetStoreDepartmentsQuery(IncludeInactive: true))).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load departments: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var nextOrder = _departments.Count == 0 ? 0 : _departments.Max(d => d.DisplayOrder) + 1;
        var model = new StoreDepartmentFormModel { DisplayOrder = nextOrder };
        var result = await DialogService.ShowAsync<StoreDepartmentFormDialog, StoreDepartmentFormModel>(model, new BoDialogOptions
        {
            Title = "New department",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreDepartmentFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Department '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(StoreDepartmentDto d)
    {
        var model = new StoreDepartmentFormModel
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            BanglaName = d.BanglaName,
            Description = d.Description,
            DisplayOrder = d.DisplayOrder
        };
        var result = await DialogService.ShowAsync<StoreDepartmentFormDialog, StoreDepartmentFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit · {d.Name}",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreDepartmentFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Department updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(StoreDepartmentDto d, bool active)
    {
        try
        {
            await Mediator.Send(new SetStoreDepartmentActiveCommand(d.Id, active));
            ToastService.ShowSuccess($"Department '{d.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
