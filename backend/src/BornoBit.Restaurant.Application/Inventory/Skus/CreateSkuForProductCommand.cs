using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Skus;

/// <summary>
/// Creates a finished-good stock SKU and links it to one product/variant in a single step (the Product SKUs
/// page). Unlike <c>SetProductInventoryMethodCommand</c> it touches only this slot, so sibling variants keep
/// their links. Sets the product to <see cref="InventoryMethod.DirectStock"/> so consumption uses the SKU.
/// </summary>
public record CreateSkuForProductCommand(
    Guid ProductId,
    Guid? VariantId,
    string Code,
    string Name,
    string? BanglaName,
    Guid InventoryCategoryId,
    Guid BaseUnitId,
    decimal ReorderLevel,
    decimal ReorderQty,
    bool IsPerishable,
    decimal? PackSize,
    string? PackNote
) : IRequest<Guid>;

public class CreateSkuForProductCommandValidator : AbstractValidator<CreateSkuForProductCommand>
{
    public CreateSkuForProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BanglaName).MaximumLength(200);
        RuleFor(x => x.InventoryCategoryId).NotEmpty();
        RuleFor(x => x.BaseUnitId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderQty).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PackSize).GreaterThan(0).When(x => x.PackSize.HasValue);
        RuleFor(x => x.PackNote).MaximumLength(200);
    }
}

public class CreateSkuForProductCommandHandler : IRequestHandler<CreateSkuForProductCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateSkuForProductCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateSkuForProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        if (request.VariantId is { } vid && product.Variants.All(v => v.Id != vid))
            throw new ConflictException("The chosen variant does not belong to this product.");

        if (!await _db.InventoryCategories.AnyAsync(c => c.Id == request.InventoryCategoryId, cancellationToken))
            throw new NotFoundException($"Inventory category {request.InventoryCategoryId} not found.");
        if (!await _db.Units.AnyAsync(u => u.Id == request.BaseUnitId, cancellationToken))
            throw new NotFoundException($"Unit {request.BaseUnitId} not found.");

        // One SKU per (product, variant) slot.
        if (await _db.InventoryItems.AnyAsync(i => i.ProductId == product.Id && i.VariantId == request.VariantId, cancellationToken))
            throw new ConflictException("This product/variant already has a stock SKU.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.InventoryItems.AnyAsync(i => i.Code == code, cancellationToken))
            throw new ValidationException($"A stock item with code '{code}' already exists.");

        var item = InventoryItem.Create(
            code,
            request.Name,
            request.InventoryCategoryId,
            InventoryItemType.FinishedGood,
            request.BaseUnitId,
            request.BanglaName,
            request.ReorderLevel,
            request.ReorderQty,
            request.IsPerishable,
            productId: product.Id,
            request.PackSize,
            request.PackNote);
        item.LinkToProduct(product.Id, request.VariantId);

        // The product now deducts a stock item when sold; idempotent if already DirectStock.
        product.SetInventoryMethod(InventoryMethod.DirectStock);

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }
}
