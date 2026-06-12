using BornoBit.Restaurant.Application.Products;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Inventory.Products;

public partial class Products : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<ProductDto> _products = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _products = (await Mediator.Send(new GetProductsQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load products: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ShowCreateAsync()
    {
        var nextOrder = _products.Count == 0 ? 0 : _products.Max(p => p.DisplayOrder) + 1;
        var model = new ProductFormModel { DisplayOrder = nextOrder };
        var result = await DialogService.ShowAsync<ProductFormDialog, ProductFormModel>(model, new BoDialogOptions
        {
            Title = "New product",
            Width = "560px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is ProductFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"Product '{saved.Name}' created.");
            await ReloadAsync();
        }
    }

    private async Task ShowEditAsync(ProductDto p)
    {
        var model = new ProductFormModel
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            BanglaName = p.BanglaName,
            ProductCategoryId = p.ProductCategoryId,
            Price = p.Price,
            Description = p.Description,
            ImagePath = p.ImagePath,
            DisplayOrder = p.DisplayOrder,
            Variants = p.Variants
                .Select(v => new VariantFormRow { Id = v.Id, Name = v.Name, Price = v.Price })
                .ToList()
        };
        var result = await DialogService.ShowAsync<ProductFormDialog, ProductFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit product · {p.Name}",
            Width = "560px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is ProductFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess("Product updated.");
            await ReloadAsync();
        }
    }

    private async Task ToggleActiveAsync(ProductDto p, bool active)
    {
        try
        {
            await Mediator.Send(new SetProductActiveCommand(p.Id, active));
            ToastService.ShowSuccess($"Product '{p.Name}' {(active ? "activated" : "deactivated")}.");
            await ReloadAsync();
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
