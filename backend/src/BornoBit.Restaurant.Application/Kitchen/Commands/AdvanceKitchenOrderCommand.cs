using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Application.Kitchen.Commands;

/// <summary>
/// Single-click kitchen advance through the fulfilment track: Confirmed → Preparing → Ready → Served.
/// Accepting a still-Placed order is a separate step (<see cref="AcceptKitchenOrderCommand"/>), so this
/// no longer auto-confirms. On Served it completes the order if it is already paid (Completed = Served
/// AND Paid). Returns the order's new status so the caller can react.
/// </summary>
public record AdvanceKitchenOrderCommand(Guid OrderId) : IRequest<OrderStatus>;

public class AdvanceKitchenOrderCommandValidator : AbstractValidator<AdvanceKitchenOrderCommand>
{
    public AdvanceKitchenOrderCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

public class AdvanceKitchenOrderCommandHandler : IRequestHandler<AdvanceKitchenOrderCommand, OrderStatus>
{
    private readonly IAppDbContext _db;
    private readonly IStockConsumptionService _consumption;
    private readonly ILogger<AdvanceKitchenOrderCommandHandler> _logger;

    public AdvanceKitchenOrderCommandHandler(
        IAppDbContext db, IStockConsumptionService consumption, ILogger<AdvanceKitchenOrderCommandHandler> logger)
    {
        _db = db;
        _consumption = consumption;
        _logger = logger;
    }

    public async Task<OrderStatus> Handle(AdvanceKitchenOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order is null) throw new NotFoundException("Order not found.");

        var startedCooking = false;
        try
        {
            switch (order.Status)
            {
                case OrderStatus.Confirmed:
                    order.StartPreparing();
                    startedCooking = true;
                    break;
                case OrderStatus.Preparing:
                    order.MarkReady();
                    break;
                case OrderStatus.Ready:
                    order.MarkServed();
                    // Completed = Served AND Paid: if the order is already paid, serving completes it.
                    if (order.IsPaid) order.Complete();
                    break;
                case OrderStatus.Placed:
                    throw new ConflictException($"Order {order.OrderNumber} must be accepted before it can be started.");
                default:
                    throw new ConflictException($"Order {order.OrderNumber} cannot be advanced from {order.Status}.");
            }
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Kitchen started this order → deduct stock (no-op if Confirm already did it).
        if (startedCooking)
            await OrderStockSync.TryApplyAsync(_db, _consumption, order, _logger, cancellationToken);

        return order.Status;
    }
}
