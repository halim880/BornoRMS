using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

/// <summary>
/// Voids (removes) a single line from an unpaid order, recording who/why. Distinct from a quantity edit:
/// it is a controlled action restricted to authorized staff and leaves an audit trail in the kitchen notes.
/// </summary>
public record VoidOrderLineCommand(Guid OrderId, Guid LineId, string Reason) : IRequest<Unit>;

public class VoidOrderLineCommandValidator : AbstractValidator<VoidOrderLineCommand>
{
    public VoidOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("A reason is required to void an item.");
    }
}

public class VoidOrderLineCommandHandler : IRequestHandler<VoidOrderLineCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStockConsumptionService _consumption;

    public VoidOrderLineCommandHandler(IAppDbContext db, ICurrentUser currentUser, IStockConsumptionService consumption)
    {
        _db = db;
        _currentUser = currentUser;
        _consumption = consumption;
    }

    public async Task<Unit> Handle(VoidOrderLineCommand request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager);

        var order = await _db.Orders.Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        if (order.IsPaid) throw new ConflictException("Cannot void items on a paid order.");

        var line = order.Lines.FirstOrDefault(l => l.Id == request.LineId)
            ?? throw new NotFoundException("Order line not found.");

        var stamp = $"[VOID] {line.Name} x{line.Quantity} by {_currentUser.UserName}: {request.Reason.Trim()}";
        var note = string.IsNullOrWhiteSpace(order.KitchenNotes) ? stamp : $"{order.KitchenNotes}\n{stamp}";

        try
        {
            // If this order already deducted stock (confirmed), restore just this line's share before
            // removing it — otherwise its consumption would be orphaned in the ledger.
            if (order.StockSyncStatus == StockSyncStatus.Synced)
                await _consumption.ReverseLineAsync(_db, order, line, cancellationToken);

            order.RemoveLine(request.LineId);
            order.UpdateKitchenNotes(note);
        }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
