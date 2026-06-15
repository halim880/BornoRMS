using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

public record PlaceOrderLineInput(Guid MenuItemId, int Quantity, Guid? VariantId = null, string? Notes = null,
    IReadOnlyList<Guid>? OptionIds = null);

public record PlaceOrderCommand(
    Guid CustomerId,
    Guid? TableId,
    OrderType Type,
    string? Notes,
    IReadOnlyList<PlaceOrderLineInput> Lines) : IRequest<PlaceOrderResult>;

public record PlaceOrderResult(Guid OrderId, string OrderNumber, decimal Total, string Currency);

public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.MenuItemId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(1);
        });
        RuleFor(x => x.TableId)
            .NotNull()
            .When(x => x.Type == OrderType.DineIn)
            .WithMessage("Dine-in orders require a table.");
    }
}

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly IAppDbContext _db;
    private readonly IOrderNumberGenerator _numbers;
    private readonly TimeProvider _timeProvider;

    public PlaceOrderCommandHandler(IAppDbContext db, IOrderNumberGenerator numbers, TimeProvider timeProvider)
    {
        _db = db;
        _numbers = numbers;
        _timeProvider = timeProvider;
    }

    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId && c.IsActive, cancellationToken);
        if (!customerExists) throw new NotFoundException("Customer not found.");

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

        // Dine-in QR self-orders are trusted (the guest is at a table); remote orders need staff to accept.
        var channel = request.Type == OrderType.DineIn ? OrderChannel.Qr : OrderChannel.Online;
        var order = Order.Create(orderNumber, request.CustomerId, request.TableId, request.Type, nowUtc, currency, request.Notes, channel);

        // Merge duplicate product+variant+options into single lines with summed quantity. Lines with
        // different add-on selections stay distinct (the options key is part of the grouping).
        foreach (var group in request.Lines.GroupBy(l => (l.MenuItemId, l.VariantId, OptionsKey(l))))
        {
            var (name, price, lineCurrency, code, stationId, prepMinutes) = OrderLineResolver.Resolve(products, group.Key.MenuItemId, group.Key.VariantId);
            var qty = group.Sum(l => l.Quantity);
            var stationName = stationId is { } sid && stationNames.TryGetValue(sid, out var sn) ? sn : null;
            var notes = group.Select(l => l.Notes).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
            var line = order.AddLine(group.Key.MenuItemId, code, name, price, lineCurrency, qty, group.Key.VariantId, stationId, stationName, notes, prepMinutes);

            var modifiers = OrderLineResolver.ResolveModifiers(products, group.Key.MenuItemId, group.First().OptionIds);
            foreach (var m in modifiers)
                line.AddModifier(m.OptionId, m.GroupName, m.OptionName, m.PriceDelta);
        }

        // QR (dine-in) orders auto-accept on placement so the kitchen ticket fires immediately; the API
        // host has the no-op KOT sender, so the actual print is picked up by the Web retry worker.
        if (channel == OrderChannel.Qr)
            order.Confirm();

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }

    /// <summary>Stable key of a line's chosen add-on options, so identical selections merge and differing ones don't.</summary>
    internal static string OptionsKey(PlaceOrderLineInput line) =>
        line.OptionIds is { Count: > 0 } ids ? string.Join(",", ids.Distinct().OrderBy(x => x)) : "";
}
