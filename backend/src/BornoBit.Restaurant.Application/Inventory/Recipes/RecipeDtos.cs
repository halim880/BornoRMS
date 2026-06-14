namespace BornoBit.Restaurant.Application.Inventory.Recipes;

/// <summary>One ingredient row of a recipe, joined to its item + unit names for display.</summary>
public record RecipeItemDto(
    Guid Id, Guid InventoryItemId, string ItemCode, string ItemName, decimal Quantity, Guid UnitId, string UnitCode);

/// <summary>Full recipe (BOM) for a product, for the editor.</summary>
public record RecipeDto(
    Guid Id, Guid ProductId, string ProductName, Guid? VariantId, decimal Yield, bool IsActive,
    IReadOnlyList<RecipeItemDto> Items);

/// <summary>An ingredient row submitted from the editor. Id null = new row.</summary>
public record RecipeItemInput(Guid? Id, Guid InventoryItemId, decimal Quantity, Guid UnitId);

/// <summary>Index row: a recipe-based product and how many ingredients it has.</summary>
public record RecipeListRowDto(Guid ProductId, string ProductCode, string ProductName, decimal Yield, int ItemCount, bool IsActive);
