using BornoBit.Restaurant.Application.Dining.Commands;
using BornoBit.Restaurant.Application.Dining.Queries;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Inventory.Tables;

public partial class Tables : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<TableAdminDto> _tables = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _tables = (await Mediator.Send(new GetAllTablesQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load tables: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var model = new TableFormModel();
        var result = await DialogService.ShowAsync<TableFormDialog, TableFormModel>(model, new BoDialogOptions
        {
            Title = "New table",
            Width = "420px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is TableFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Table '{saved.TableNumber}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(TableAdminDto t)
    {
        var model = new TableFormModel
        {
            Id = t.Id,
            TableNumber = t.TableNumber,
            Capacity = t.Capacity
        };
        var result = await DialogService.ShowAsync<TableFormDialog, TableFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit table · {t.TableNumber}",
            Width = "420px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is TableFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Table updated.");
            await ReloadAsync();
        }
    }

    private async Task ShowQrAsync(TableAdminDto t)
    {
        var model = new TableQrModel { Id = t.Id, TableNumber = t.TableNumber };
        await DialogService.ShowAsync<TableQrDialog, TableQrModel>(model, new BoDialogOptions
        {
            Title = $"QR code · {t.TableNumber}",
            Width = "420px"
        });
    }

    private async Task ToggleActiveAsync(TableAdminDto t, bool active)
    {
        try
        {
            await Mediator.Send(new SetTableActiveCommand(t.Id, active));
            ToastService.ShowSuccess($"Table '{t.TableNumber}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
