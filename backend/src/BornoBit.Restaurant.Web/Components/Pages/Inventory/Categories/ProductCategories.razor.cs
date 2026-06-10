using BornoBit.Restaurant.Application.ProductCategories;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Inventory.Categories;

public partial class ProductCategories : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<ProductCategoryDto> _categories = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _categories = (await Mediator.Send(new GetProductCategoriesQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load categories: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var nextOrder = _categories.Count == 0 ? 0 : _categories.Max(c => c.DisplayOrder) + 1;
        var model = new ProductCategoryFormModel { DisplayOrder = nextOrder };
        var result = await DialogService.ShowAsync<ProductCategoryFormDialog, ProductCategoryFormModel>(model, new BoDialogOptions
        {
            Title = "New category",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is ProductCategoryFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Category '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(ProductCategoryDto c)
    {
        var model = new ProductCategoryFormModel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            DisplayOrder = c.DisplayOrder
        };
        var result = await DialogService.ShowAsync<ProductCategoryFormDialog, ProductCategoryFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit category · {c.Name}",
            Width = "480px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is ProductCategoryFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Category updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(ProductCategoryDto c, bool active)
    {
        try
        {
            await Mediator.Send(new SetProductCategoryActiveCommand(c.Id, active));
            ToastService.ShowSuccess($"Category '{c.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
