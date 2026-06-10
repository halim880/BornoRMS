using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Products;

public record SetProductActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetProductActiveCommandValidator : AbstractValidator<SetProductActiveCommand>
{
    public SetProductActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetProductActiveCommandHandler : IRequestHandler<SetProductActiveCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetProductActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetProductActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product {request.Id} not found.");

        if (request.IsActive) entity.Activate();
        else entity.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
