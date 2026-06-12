using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Dining.Commands;

public record SetTableActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetTableActiveCommandValidator : AbstractValidator<SetTableActiveCommand>
{
    public SetTableActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetTableActiveCommandHandler : IRequestHandler<SetTableActiveCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetTableActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetTableActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.RestaurantTables
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Table {request.Id} not found.");

        if (request.IsActive) entity.Activate();
        else entity.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
