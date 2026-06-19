using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Application.Ordering.Common;
using BornoBit.Restaurant.Domain.Logistics;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Application.Ordering.Pos;

/// <summary>
/// POS order instantiation: creates an EMPTY order (zero lines) of the given type so the cashier
/// can start appending products immediately. Unlike <see cref="PlaceWaiterOrderCommand"/>, no lines
/// are required up front. Customer is resolved find-or-create by phone (shared walk-in when no
/// phone is given); the address is stored on the customer for real phones, or in the order notes
/// for anonymous walk-ins so delivery details are never lost.
/// </summary>
public record CreatePosOrderCommand(
    OrderType Type,
    Guid? TableId,
    string? CustomerPhone,
    string? CustomerName,
    string? CustomerAddress,
    decimal DeliveryCharge = 0m) : IRequest<PlaceOrderResult>;

public class CreatePosOrderCommandValidator : AbstractValidator<CreatePosOrderCommand>
{
    public CreatePosOrderCommandValidator()
    {
        RuleFor(x => x.TableId)
            .NotNull()
            .When(x => x.Type == OrderType.DineIn)
            .WithMessage("Dine-in orders require a table.");
        RuleFor(x => x.CustomerAddress)
            .NotEmpty()
            .When(x => x.Type == OrderType.Delivery)
            .WithMessage("Delivery orders require an address.");
        RuleFor(x => x.CustomerPhone).MaximumLength(40);
        RuleFor(x => x.CustomerName).MaximumLength(200);
        RuleFor(x => x.CustomerAddress).MaximumLength(500);
        RuleFor(x => x.DeliveryCharge).GreaterThanOrEqualTo(0m);
    }
}

public class CreatePosOrderCommandHandler : IRequestHandler<CreatePosOrderCommand, PlaceOrderResult>
{
    private readonly IAppDbContext _db;
    private readonly IOrderNumberGenerator _numbers;
    private readonly IDineInSessionResolver _sessions;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreatePosOrderCommandHandler(IAppDbContext db, IOrderNumberGenerator numbers, IDineInSessionResolver sessions, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _db = db;
        _numbers = numbers;
        _sessions = sessions;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<PlaceOrderResult> Handle(CreatePosOrderCommand request, CancellationToken cancellationToken)
    {
        var customerId = await PosCustomerResolver.ResolveAsync(
            _db, request.CustomerPhone, request.CustomerName, request.CustomerAddress, cancellationToken);

        if (request.TableId is { } tableId)
            await PosTableGuard.EnsureAvailableAsync(_db, tableId, currentOrderId: null, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var orderNumber = await _numbers.NextAsync(nowUtc, cancellationToken);

        // Anonymous walk-in customers are shared, so an address given without a phone is kept on the order.
        var notes = string.IsNullOrWhiteSpace(request.CustomerPhone) && !string.IsNullOrWhiteSpace(request.CustomerAddress)
            ? $"Address: {request.CustomerAddress.Trim()}"
            : null;

        var order = Order.Create(orderNumber, customerId, request.TableId, request.Type, nowUtc, notes: notes, channel: OrderChannel.Pos);

        // Dine-in orders join a dining session so the table reads as occupied on the Waiter/Dashboard floor too.
        if (request.Type == OrderType.DineIn && request.TableId is { } dineTableId)
        {
            order.AssignWaiter(_currentUser.UserId, _currentUser.UserName);
            var sessionId = await _sessions.ResolveAsync(_db, dineTableId, null, null, nowUtc, cancellationToken);
            order.AttachToSession(sessionId);
        }

        _db.Orders.Add(order);

        // Delivery orders carry a fee on the bill and open a dispatch/tracking record.
        if (request.Type == OrderType.Delivery)
        {
            order.SetDeliveryCharge(request.DeliveryCharge);
            _db.Deliveries.Add(Delivery.Create(order.Id, request.CustomerAddress!.Trim(), request.CustomerPhone));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }
}
