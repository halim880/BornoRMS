using BornoBit.Restaurant.Application.Accounting.Categories;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

public partial class Categories : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<CategoryDto> _categories = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _categories = (await Mediator.Send(new GetCategoriesQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load categories: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var model = new CategoryFormModel();
        var result = await DialogService.ShowAsync<CategoryFormDialog, CategoryFormModel>(model, new BoDialogOptions
        {
            Title = "New category",
            Width = "440px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is CategoryFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Category '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(CategoryDto c)
    {
        var model = new CategoryFormModel
        {
            Id = c.Id,
            Name = c.Name,
            Type = c.Type
        };
        var result = await DialogService.ShowAsync<CategoryFormDialog, CategoryFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit · {c.Name}",
            Width = "440px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is CategoryFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Category updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(CategoryDto c, bool active)
    {
        try
        {
            await Mediator.Send(new SetCategoryActiveCommand(c.Id, active));
            ToastService.ShowSuccess($"Category '{c.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
