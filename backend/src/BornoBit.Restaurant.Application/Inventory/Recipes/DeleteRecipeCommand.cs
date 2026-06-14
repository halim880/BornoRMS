using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Inventory.Recipes;

/// <summary>Deletes a recipe and resets its product back to <see cref="InventoryMethod.None"/>.</summary>
public record DeleteRecipeCommand(Guid RecipeId) : IRequest<Unit>;

public class DeleteRecipeCommandValidator : AbstractValidator<DeleteRecipeCommand>
{
    public DeleteRecipeCommandValidator() => RuleFor(x => x.RecipeId).NotEmpty();
}

public class DeleteRecipeCommandHandler : IRequestHandler<DeleteRecipeCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DeleteRecipeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _db.Recipes
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Recipe {request.RecipeId} not found.");

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == recipe.ProductId, cancellationToken);
        product?.SetInventoryMethod(InventoryMethod.None);

        _db.RecipeItems.RemoveRange(recipe.Items);
        _db.Recipes.Remove(recipe);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
