using BornoBit.Restaurant.Application.Inventory.Skus;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class Skus : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<ProductSkusDto> _products = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _products = (await Mediator.Send(new GetProductSkusQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load products: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task AddSkuAsync(ProductSkusDto product, SkuSlotDto slot)
    {
        // Suggest a code/name from the product + variant; both stay editable in the dialog.
        var suffix = slot.VariantName is null ? "" : $"-{Slug(slot.VariantName)}";
        var model = new SkuFormModel
        {
            ProductId = product.ProductId,
            VariantId = slot.VariantId,
            ProductName = product.Name,
            VariantName = slot.VariantName,
            Code = $"{product.Code}{suffix}".ToUpperInvariant(),
            Name = slot.VariantName is null ? product.Name : $"{product.Name} {slot.VariantName}",
        };

        var title = slot.VariantName is null ? $"New SKU · {product.Name}" : $"New SKU · {product.Name} · {slot.VariantName}";
        var result = await DialogService.ShowAsync<SkuFormDialog, SkuFormModel>(model, new BoDialogOptions
        {
            Title = title,
            Width = "560px",
            DismissOnOverlayClick = false
        });

        if (!result.Cancelled && result.Data is SkuFormModel saved && saved.SavedId.HasValue)
        {
            ToastService.ShowSuccess($"SKU '{saved.Code}' created.");
            await ReloadAsync();
        }
    }

    private static string Slug(string s) =>
        new string(s.Where(ch => char.IsLetterOrDigit(ch)).ToArray());
}
