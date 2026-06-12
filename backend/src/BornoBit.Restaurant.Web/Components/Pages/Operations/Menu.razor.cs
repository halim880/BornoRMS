using BornoBit.Restaurant.Application.ProductCategories;
using BornoBit.Restaurant.Application.Products;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Operations;

public partial class Menu : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private string? _search;
    private bool _unavailableOnly;
    private Guid? _busyId;
    private List<ProductDto> _products = new();
    private IReadOnlyList<ProductCategoryDto> _categories = Array.Empty<ProductCategoryDto>();

    private IEnumerable<ProductDto> Filtered => _products.Where(p =>
        (!_unavailableOnly || !p.IsActive)
        && (string.IsNullOrWhiteSpace(_search)
            || p.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || p.Code.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || (p.BanglaName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false)
            || p.CategoryName.Contains(_search, StringComparison.OrdinalIgnoreCase)));

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _products = (await Mediator.Send(new GetProductsQuery())).ToList();
            _categories = await Mediator.Send(new GetProductCategoriesQuery());
        }
        catch (Exception ex) { _error = $"Failed to load menu: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task ToggleAsync(ProductDto p)
    {
        var target = !p.IsActive;
        _busyId = p.Id;
        try
        {
            await Mediator.Send(new SetProductActiveCommand(p.Id, target));
            // Patch in place instead of reloading so scroll/search state survives rapid toggling.
            var i = _products.FindIndex(x => x.Id == p.Id);
            if (i >= 0) { _products[i] = p with { IsActive = target }; }
            ToastService.ShowSuccess($"'{p.Name}' marked {(target ? "available" : "unavailable")}.");
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
        finally { _busyId = null; }
    }
}
