using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Suppliers;

public record SetSupplierActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetSupplierActiveCommandValidator : AbstractValidator<SetSupplierActiveCommand>
{
    public SetSupplierActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetSupplierActiveCommandHandler : IRequestHandler<SetSupplierActiveCommand, Unit>
{
    private readonly IAppDbContext _db;

    public SetSupplierActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(SetSupplierActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Supplier {request.Id} not found.");

        if (request.IsActive) entity.Activate();
        else entity.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
