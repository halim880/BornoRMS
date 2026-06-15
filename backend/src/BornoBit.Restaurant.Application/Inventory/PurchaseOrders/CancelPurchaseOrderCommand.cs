using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

/// <summary>Cancel a purchase order that is not already fully received.</summary>
public record CancelPurchaseOrderCommand(Guid Id) : IRequest<Unit>;

public class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class CancelPurchaseOrderCommandHandler : IRequestHandler<CancelPurchaseOrderCommand, Unit>
{
    private readonly IAppDbContext _db;

    public CancelPurchaseOrderCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Purchase order {request.Id} not found.");

        try { po.Cancel(); }
        catch (InvalidOperationException ex) { throw new ValidationException(ex.Message); }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
