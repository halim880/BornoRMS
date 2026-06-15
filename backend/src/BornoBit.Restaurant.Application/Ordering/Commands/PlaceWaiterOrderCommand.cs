using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Ordering.Common;
using BornoBit.Restaurant.Application.Ordering.Printing;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

/// <summary>
/// Staff/waiter order entry. Resolves the customer (find-or-create by phone, or the shared walk-in
/// customer when no phone is given) then builds the order from the <c>Products</c> catalog. Each
/// <see cref="PlaceOrderLineInput.MenuItemId"/> here carries a <c>Product</c> id — the same
/// convention the customer flow's <see cref="PlaceOrderCommand"/> uses.
/// </summary>
public record PlaceWaiterOrderCommand(
    string? CustomerPhone,
    string? CustomerName,
    Guid? TableId,
    OrderType Type,
    string? Notes,
    IReadOnlyList<PlaceOrderLineInput> Lines,
    int? GuestCount = null,
    Guid? DiningSessionId = null) : IRequest<PlaceOrderResult>;

public class PlaceWaiterOrderCommandValidator : AbstractValidator<PlaceWaiterOrderCommand>
{
    public PlaceWaiterOrderCommandValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item is required.");
        RuleFor(x => x.TableId)
            .NotNull()
            .When(x => x.Type == OrderType.DineIn)
            .WithMessage("Dine-in orders require a table.");
    }
}

public class PlaceWaiterOrderCommandHandler : IRequestHandler<PlaceWaiterOrderCommand, PlaceOrderResult>
{
    private readonly IAppDbContext _db;
    private readonly IOrderNumberGenerator _numbers;
    private readonly IDineInSessionResolver _sessions;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;
    private readonly IKitchenTicketSender _kot;
    private readonly ILogger<PlaceWaiterOrderCommandHandler> _logger;

    public PlaceWaiterOrderCommandHandler(IAppDbContext db, IOrderNumberGenerator numbers, IDineInSessionResolver sessions, TimeProvider timeProvider, ICurrentUser currentUser, IKitchenTicketSender kot, ILogger<PlaceWaiterOrderCommandHandler> logger)
    {
        _db = db;
        _numbers = numbers;
        _sessions = sessions;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
        _kot = kot;
        _logger = logger;
    }

    public async Task<PlaceOrderResult> Handle(PlaceWaiterOrderCommand request, CancellationToken cancellationToken)
    {
        var customerId = await ResolveCustomerIdAsync(request.CustomerPhone, request.CustomerName, cancellationToken);

        if (request.TableId is { } tableId)
        {
            var tableOk = await _db.RestaurantTables.AnyAsync(t => t.Id == tableId && t.IsActive, cancellationToken);
            if (!tableOk) throw new NotFoundException("Table not found.");
        }

        var productIds = request.Lines.Select(l => l.MenuItemId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.Variants)
            .Include(p => p.OptionGroups).ThenInclude(g => g.Options)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var stationNames = await _db.KitchenStations.ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        foreach (var lineInput in request.Lines)
        {
            OrderLineResolver.Resolve(products, lineInput.MenuItemId, lineInput.VariantId);
            OrderLineResolver.ResolveModifiers(products, lineInput.MenuItemId, lineInput.OptionIds);
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var orderNumber = await _numbers.NextAsync(nowUtc, cancellationToken);
        var currency = products[request.Lines[0].MenuItemId].Currency;

        var order = Order.Create(orderNumber, customerId, request.TableId, request.Type, nowUtc, currency, request.Notes, OrderChannel.Waiter);
        order.AssignWaiter(_currentUser.UserId, _currentUser.UserName);
        order.SetGuestCount(request.GuestCount);

        // Dine-in orders belong to a dining session: use the one supplied, the table's open one, or open a new one.
        if (request.Type == OrderType.DineIn && request.TableId is { } dineTableId)
        {
            var sessionId = await _sessions.ResolveAsync(_db, dineTableId, request.DiningSessionId, request.GuestCount, nowUtc, cancellationToken);
            order.AttachToSession(sessionId);
        }

        // Merge duplicate product+variant pairs into single lines with summed quantity. The Product id
        // is stored in OrderLine.MenuItemId (an unconstrained Guid) alongside a snapshot of code/name/price.
        foreach (var group in request.Lines.GroupBy(l => (l.MenuItemId, l.VariantId, PlaceOrderCommandHandler.OptionsKey(l))))
        {
            var (name, price, currency2, code, stationId, prepMinutes) = OrderLineResolver.Resolve(products, group.Key.MenuItemId, group.Key.VariantId);
            var qty = group.Sum(l => l.Quantity);
            var stationName = stationId is { } sid && stationNames.TryGetValue(sid, out var sn) ? sn : null;
            var notes = group.Select(l => l.Notes).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
            var line = order.AddLine(group.Key.MenuItemId, code, name, price, currency2, qty, group.Key.VariantId, stationId, stationName, notes, prepMinutes);

            var modifiers = OrderLineResolver.ResolveModifiers(products, group.Key.MenuItemId, group.First().OptionIds);
            foreach (var m in modifiers)
                line.AddModifier(m.OptionId, m.GroupName, m.OptionName, m.PriceDelta);
        }

        // Waiter orders are trusted in-house: auto-accept on placement (instant accepted ticket + ETA).
        order.Confirm();

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        // Accept = fire: dispatch the kitchen ticket (idempotent; no-op transport in the API).
        await OrderKotSync.TryDispatchAsync(_db, _kot, order, _logger, cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }

    private async Task<Guid> ResolveCustomerIdAsync(string? phone, string? name, CancellationToken cancellationToken)
    {
        var lookup = string.IsNullOrWhiteSpace(phone) ? Customer.WalkInPhone : phone.Trim();

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == lookup, cancellationToken);
        if (customer is null)
        {
            customer = Customer.Create(lookup, name);
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(customer.FullName)
                 && lookup != Customer.WalkInPhone)
        {
            customer.UpdateName(name);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return customer.Id;
    }
}
