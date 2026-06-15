using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

/// <summary>Approve a Draft purchase order so goods receipts can be raised against it.</summary>
public record ApprovePurchaseOrderCommand(Guid Id) : IRequest<Unit>;

public class ApprovePurchaseOrderCommandValidator : AbstractValidator<ApprovePurchaseOrderCommand>
{
    public ApprovePurchaseOrderCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class ApprovePurchaseOrderCommandHandler : IRequestHandler<ApprovePurchaseOrderCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ApprovePurchaseOrderCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Purchase order {request.Id} not found.");

        try { po.Approve(_timeProvider.GetUtcNow().UtcDateTime); }
        catch (InvalidOperationException ex) { throw new ValidationException(ex.Message); }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
