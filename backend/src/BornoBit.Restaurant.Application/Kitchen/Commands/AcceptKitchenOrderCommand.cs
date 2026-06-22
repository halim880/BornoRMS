using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Application.Ordering.Printing;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Application.Kitchen.Commands;

/// <summary>
/// Explicitly accepts a still-<see cref="OrderStatus.Placed"/> order (Online/POS) — the "Accept" action
/// on the Kitchen Display, distinct from "Start cooking". Confirms the order (= fires the ticket) and
/// dispatches the KOT. QR/Waiter orders are auto-accepted at placement and never hit this path.
/// </summary>
public record AcceptKitchenOrderCommand(Guid OrderId) : IRequest<OrderStatus>;

public class AcceptKitchenOrderCommandValidator : AbstractValidator<AcceptKitchenOrderCommand>
{
    public AcceptKitchenOrderCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

public class AcceptKitchenOrderCommandHandler : IRequestHandler<AcceptKitchenOrderCommand, OrderStatus>
{
    private readonly IAppDbContext _db;
    private readonly IStockConsumptionService _consumption;
    private readonly IKitchenTicketSender _kot;
    private readonly ILogger<AcceptKitchenOrderCommandHandler> _logger;

    public AcceptKitchenOrderCommandHandler(
        IAppDbContext db, IStockConsumptionService consumption, IKitchenTicketSender kot, ILogger<AcceptKitchenOrderCommandHandler> logger)
    {
        _db = db;
        _consumption = consumption;
        _kot = kot;
        _logger = logger;
    }

    public async Task<OrderStatus> Handle(AcceptKitchenOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order is null) throw new NotFoundException("Order not found.");

        try
        {
            order.Confirm();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Accept = fire: the kitchen now owns the order, so deduct its stock here too (the other confirm
        // paths — ChangeOrderStatus→Confirmed and POS quick-pay — already do). Idempotent + failure-tolerant,
        // so it's a no-op if the order was already deducted.
        await OrderStockSync.TryApplyAsync(_db, _consumption, order, _logger, cancellationToken);

        // Dispatch the kitchen ticket (idempotent + failure-tolerant).
        await OrderKotSync.TryDispatchAsync(_db, _kot, order, _logger, cancellationToken);

        return order.Status;
    }
}
