namespace BornoBit.Restaurant.Domain.Catalog;

/// <summary>
/// How selling a <see cref="Product"/> impacts stock.
/// <list type="bullet">
/// <item><see cref="None"/> — no stock effect (service items: delivery fee, service charge).</item>
/// <item><see cref="DirectStock"/> — deduct the linked <c>InventoryItem</c> directly (bottled drinks, packaged goods).</item>
/// <item><see cref="RecipeBased"/> — explode the product's <c>Recipe</c> (BOM) and deduct each ingredient.</item>
/// </list>
/// </summary>
public enum InventoryMethod
{
    None = 0,
    DirectStock = 1,
    RecipeBased = 2
}
