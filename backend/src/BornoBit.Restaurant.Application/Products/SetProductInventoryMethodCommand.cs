using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Products;

/// <summary>
/// Sets how selling a product impacts stock. For <see cref="InventoryMethod.DirectStock"/>, links the
/// chosen <c>InventoryItem</c> to this product (so consumption can find it). RecipeBased is configured via
/// the recipe editor (<c>UpsertRecipeCommand</c>); this command just sets the flag.
/// </summary>
public record SetProductInventoryMethodCommand(Guid ProductId, InventoryMethod Method, Guid? LinkedInventoryItemId)
    : IRequest<Unit>;

public class SetProductInventoryMethodCommandValidator : AbstractValidator<SetProductInventoryMethodCommand>
{
    public SetProductInventoryMethodCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.LinkedInventoryItemId)
            .NotEmpty().When(x => x.Method == InventoryMethod.DirectStock)
            .WithMessage("Direct-stock products must be linked to a stock item.");
    }
}

public class SetProductInventoryMethodCommandHandler : IRequestHandler<SetProductInventoryMethodCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetProductInventoryMethodCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetProductInventoryMethodCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        if (request.Method == InventoryMethod.DirectStock)
        {
            var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == request.LinkedInventoryItemId, cancellationToken)
                ?? throw new NotFoundException("Linked stock item not found.");

            // Point the stock item at this product so the consumption engine can resolve it.
            item.UpdateDetails(item.Code, item.Name, item.InventoryCategoryId, item.ItemType, item.BaseUnitId,
                item.BanglaName, item.ReorderLevel, item.ReorderQty, item.IsPerishable,
                productId: product.Id, item.PackSize, item.PackNote);
        }

        product.SetInventoryMethod(request.Method);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
