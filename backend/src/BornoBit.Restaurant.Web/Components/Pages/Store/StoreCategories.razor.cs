using BornoBit.Restaurant.Application.Store.Categories;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Store;

public partial class StoreCategories : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<StoreCategoryDto> _categories = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _categories = (await Mediator.Send(new GetStoreCategoriesQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load categories: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var nextOrder = _categories.Count == 0 ? 0 : _categories.Max(c => c.DisplayOrder) + 1;
        var model = new StoreCategoryFormModel { DisplayOrder = nextOrder };
        var result = await DialogService.ShowAsync<StoreCategoryFormDialog, StoreCategoryFormModel>(model, new BoDialogOptions
        {
            Title = "New store category",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreCategoryFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Category '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(StoreCategoryDto c)
    {
        var model = new StoreCategoryFormModel
        {
            Id = c.Id,
            Name = c.Name,
            BanglaName = c.BanglaName,
            Description = c.Description,
            DisplayOrder = c.DisplayOrder
        };
        var result = await DialogService.ShowAsync<StoreCategoryFormDialog, StoreCategoryFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit · {c.Name}",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is StoreCategoryFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Category updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(StoreCategoryDto c, bool active)
    {
        try
        {
            await Mediator.Send(new SetStoreCategoryActiveCommand(c.Id, active));
            ToastService.ShowSuccess($"Category '{c.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
