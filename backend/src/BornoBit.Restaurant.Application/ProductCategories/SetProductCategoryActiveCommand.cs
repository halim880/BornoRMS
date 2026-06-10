using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.ProductCategories;

public record SetProductCategoryActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetProductCategoryActiveCommandValidator : AbstractValidator<SetProductCategoryActiveCommand>
{
    public SetProductCategoryActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetProductCategoryActiveCommandHandler : IRequestHandler<SetProductCategoryActiveCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetProductCategoryActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetProductCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product category {request.Id} not found.");

        if (request.IsActive) entity.Activate();
        else entity.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
