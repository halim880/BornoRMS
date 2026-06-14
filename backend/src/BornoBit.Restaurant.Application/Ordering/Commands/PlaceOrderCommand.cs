using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

public record PlaceOrderLineInput(Guid MenuItemId, int Quantity, Guid? VariantId = null, string? Notes = null);

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
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var stationNames = await _db.KitchenStations.ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        foreach (var lineInput in request.Lines)
            OrderLineResolver.Resolve(products, lineInput.MenuItemId, lineInput.VariantId);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var orderNumber = await _numbers.NextAsync(nowUtc, cancellationToken);
        var currency = products[request.Lines[0].MenuItemId].Currency;

        var order = Order.Create(orderNumber, request.CustomerId, request.TableId, request.Type, nowUtc, currency, request.Notes);

        // Merge duplicate product+variant pairs into single lines with summed quantity.
        foreach (var group in request.Lines.GroupBy(l => (l.MenuItemId, l.VariantId)))
        {
            var (name, price, lineCurrency, code, stationId) = OrderLineResolver.Resolve(products, group.Key.MenuItemId, group.Key.VariantId);
            var qty = group.Sum(l => l.Quantity);
            var stationName = stationId is { } sid && stationNames.TryGetValue(sid, out var sn) ? sn : null;
            var notes = group.Select(l => l.Notes).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
            order.AddLine(group.Key.MenuItemId, code, name, price, lineCurrency, qty, group.Key.VariantId, stationId, stationName, notes);
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }
}
