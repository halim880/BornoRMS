using BornoBit.Restaurant.Domain.Inventory;
using Xunit;

namespace BornoBit.Restaurant.Tests.Unit;

/// <summary>Product/variant linking on a finished-good inventory item (DirectStock).</summary>
public class InventoryItemLinkTests
{
    private static InventoryItem NewItem() => InventoryItem.Create(
        "FG-WATER-1L", "Bottled Water 1L", Guid.NewGuid(), InventoryItemType.FinishedGood, Guid.NewGuid());

    [Fact]
    public void LinkToProduct_sets_product_and_variant()
    {
        var item = NewItem();
        var product = Guid.NewGuid();
        var variant = Guid.NewGuid();

        item.LinkToProduct(product, variant);

        Assert.Equal(product, item.ProductId);
        Assert.Equal(variant, item.VariantId);
    }

    [Fact]
    public void Unlinking_clears_both_product_and_variant()
    {
        var item = NewItem();
        item.LinkToProduct(Guid.NewGuid(), Guid.NewGuid());

        item.LinkToProduct(null, Guid.NewGuid()); // null product → variant is meaningless, must clear too

        Assert.Null(item.ProductId);
        Assert.Null(item.VariantId);
    }

    [Fact]
    public void Product_level_link_leaves_variant_null()
    {
        var item = NewItem();

        item.LinkToProduct(Guid.NewGuid(), null);

        Assert.NotNull(item.ProductId);
        Assert.Null(item.VariantId);
    }
}
