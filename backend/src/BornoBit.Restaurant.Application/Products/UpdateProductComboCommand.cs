using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

/// <summary>Sets a product's combo flag and reconciles its component list.</summary>
public record UpdateProductComboCommand(
    Guid ProductId,
    bool IsCombo,
    IReadOnlyList<ComboComponentInput> Components
) : IRequest<Unit>;

public class UpdateProductComboCommandValidator : AbstractValidator<UpdateProductComboCommand>
{
    public UpdateProductComboCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleForEach(x => x.Components).ChildRules(c =>
        {
            c.RuleFor(x => x.ComponentProductId).NotEmpty();
            c.RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
        });
        RuleFor(x => x)
            .Must(x => !x.IsCombo || x.Components.Count > 0)
            .WithMessage("A combo must have at least one component.");
        RuleFor(x => x.Components)
            .Must(c => c.All(x => x.ComponentProductId != Guid.Empty))
            .WithMessage("Component product is required.");
    }
}

public class UpdateProductComboCommandHandler : IRequestHandler<UpdateProductComboCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateProductComboCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateProductComboCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.ComboComponents)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        if (request.Components.Any(c => c.ComponentProductId == request.ProductId))
            throw new ValidationException("A combo cannot contain itself.");

        var componentIds = request.Components.Select(c => c.ComponentProductId).Distinct().ToList();
        if (componentIds.Count > 0)
        {
            var foundCount = await _db.Products.CountAsync(p => componentIds.Contains(p.Id), cancellationToken);
            if (foundCount != componentIds.Count) throw new NotFoundException("One or more combo components do not exist.");
        }

        var existingIds = product.ComboComponents.Select(c => c.Id).ToHashSet();
        product.SyncComboComponents(request.Components
            .Select(c => (c.Id, c.ComponentProductId, c.Quantity, c.DisplayOrder)).ToList());
        product.SetCombo(request.IsCombo);

        foreach (var c in product.ComboComponents.Where(c => !existingIds.Contains(c.Id)))
            _db.ComboComponents.Add(c);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
