using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

/// <summary>Replaces the full modifier / add-on option-group tree for a product.</summary>
public record UpdateProductOptionGroupsCommand(
    Guid ProductId,
    IReadOnlyList<OptionGroupInput> Groups
) : IRequest<Unit>;

public class UpdateProductOptionGroupsCommandValidator : AbstractValidator<UpdateProductOptionGroupsCommand>
{
    public UpdateProductOptionGroupsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleForEach(x => x.Groups).ChildRules(g =>
        {
            g.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            g.RuleFor(x => x.BanglaName).MaximumLength(100);
            g.RuleFor(x => x.MaxSelections).GreaterThanOrEqualTo(1);
            g.RuleFor(x => x.MinSelections).GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(x => x.MaxSelections).WithMessage("Min selections cannot exceed max selections.");
            g.RuleFor(x => x.Options).NotEmpty().WithMessage("A group must have at least one option.");
            g.RuleForEach(x => x.Options).ChildRules(o =>
            {
                o.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
                o.RuleFor(x => x.BanglaName).MaximumLength(100);
                o.RuleFor(x => x.PriceDelta).GreaterThanOrEqualTo(0);
            });
        });
    }
}

public class UpdateProductOptionGroupsCommandHandler : IRequestHandler<UpdateProductOptionGroupsCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateProductOptionGroupsCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateProductOptionGroupsCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.OptionGroups).ThenInclude(g => g.Options)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        var existingGroupIds = product.OptionGroups.Select(g => g.Id).ToHashSet();
        var existingOptionIds = product.OptionGroups.SelectMany(g => g.Options).Select(o => o.Id).ToHashSet();

        product.SyncOptionGroups(request.Groups.Select(g => new OptionGroupSpec(
            g.Id, g.Name, g.BanglaName, g.MinSelections, g.MaxSelections, g.DisplayOrder,
            g.Options.Select(o => (o.Id, o.Name, o.BanglaName, o.PriceDelta, o.DisplayOrder)).ToList())).ToList());

        // The product is tracked, so new rows (pre-set Guid keys) would be saved as UPDATEs — mark Added.
        foreach (var g in product.OptionGroups.Where(g => !existingGroupIds.Contains(g.Id)))
            _db.ProductOptionGroups.Add(g);
        foreach (var o in product.OptionGroups.SelectMany(g => g.Options).Where(o => !existingOptionIds.Contains(o.Id)))
            _db.ProductOptions.Add(o);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
