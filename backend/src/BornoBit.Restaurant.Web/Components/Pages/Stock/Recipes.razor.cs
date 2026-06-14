using BornoBit.Restaurant.Application.Inventory.Recipes;
using BornoBit.Restaurant.Web.Components.BornoUi.Dialog;
using BornoBit.Restaurant.Web.Components.BornoUi.Toast;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace BornoBit.Restaurant.Web.Components.Pages.Stock;

public partial class Recipes : ComponentBase
{
    [Inject] private ISender Mediator { get; set; } = default!;
    [Inject] private IBoDialogService DialogService { get; set; } = default!;
    [Inject] private IBoToastService ToastService { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<RecipeListRowDto> _rows = new();

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _rows = (await Mediator.Send(new GetRecipesQuery())).ToList();
        }
        catch (Exception ex) { _error = $"Failed to load recipes: {ex.Message}"; }
        finally { _loading = false; }
    }

    private async Task AddAsync()
    {
        var model = new RecipeFormModel { IsExisting = false, Yield = 1m };
        var result = await DialogService.ShowAsync<RecipeFormDialog, RecipeFormModel>(model, new BoDialogOptions
        {
            Title = "New recipe",
            Width = "720px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is RecipeFormModel saved && saved.Saved)
        {
            ToastService.ShowSuccess("Recipe saved.");
            await ReloadAsync();
        }
    }

    private async Task EditAsync(Guid productId, string productName)
    {
        var existing = await Mediator.Send(new GetRecipeByProductQuery(productId));
        var model = new RecipeFormModel
        {
            ProductId = productId,
            ProductName = productName,
            IsExisting = true,
            Yield = existing?.Yield ?? 1m,
            Items = existing?.Items
                .Select(i => new RecipeFormModel.RecipeRow { Id = i.Id, InventoryItemId = i.InventoryItemId, Quantity = i.Quantity, UnitId = i.UnitId })
                .ToList() ?? new()
        };
        var result = await DialogService.ShowAsync<RecipeFormDialog, RecipeFormModel>(model, new BoDialogOptions
        {
            Title = $"Edit recipe · {productName}",
            Width = "720px",
            DismissOnOverlayClick = false
        });
        if (!result.Cancelled && result.Data is RecipeFormModel saved && saved.Saved)
        {
            ToastService.ShowSuccess("Recipe saved.");
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync(RecipeListRowDto row)
    {
        var ok = await DialogService.ConfirmAsync(
            "Delete recipe?",
            $"Remove the recipe for '{row.ProductName}'? The product will revert to no stock tracking.",
            confirmLabel: "Delete", variant: "danger");
        if (!ok) return;

        try
        {
            var existing = await Mediator.Send(new GetRecipeByProductQuery(row.ProductId));
            if (existing is not null)
            {
                await Mediator.Send(new DeleteRecipeCommand(existing.Id));
                ToastService.ShowSuccess("Recipe deleted.");
                await ReloadAsync();
            }
        }
        catch (Exception ex) { ToastService.ShowError(ex.Message); }
    }
}
