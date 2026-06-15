using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.ProductCategories;

public record UpdateProductCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    int DisplayOrder,
    decimal? TaxRatePercent = null
) : IRequest<Unit>;

public class UpdateProductCategoryCommandValidator : AbstractValidator<UpdateProductCategoryCommand>
{
    public UpdateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxRatePercent).InclusiveBetween(0, 100).When(x => x.TaxRatePercent.HasValue);
    }
}

public class UpdateProductCategoryCommandHandler : IRequestHandler<UpdateProductCategoryCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateProductCategoryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product category {request.Id} not found.");

        var name = request.Name.Trim();
        var clash = await _db.ProductCategories
            .AnyAsync(c => c.Id != request.Id && c.Name == name, cancellationToken);
        if (clash) throw new ValidationException($"A product category named '{name}' already exists.");

        entity.UpdateDetails(name, request.DisplayOrder, request.Description, request.TaxRatePercent);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
