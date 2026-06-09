using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

public record PlaceOrderLineInput(Guid MenuItemId, int Quantity);

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

        var itemIds = request.Lines.Select(l => l.MenuItemId).Distinct().ToList();
        var items = await _db.MenuItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        foreach (var lineInput in request.Lines)
        {
            if (!items.TryGetValue(lineInput.MenuItemId, out var item))
                throw new NotFoundException($"Menu item {lineInput.MenuItemId} not found.");
            if (!item.IsAvailable)
                throw new ConflictException($"Menu item '{item.Name}' is currently unavailable.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var orderNumber = await _numbers.NextAsync(nowUtc, cancellationToken);
        var currency = items[request.Lines[0].MenuItemId].Currency;

        var order = Order.Create(orderNumber, request.CustomerId, request.TableId, request.Type, nowUtc, currency, request.Notes);

        // Merge duplicate menu items into single lines with summed quantity.
        foreach (var group in request.Lines.GroupBy(l => l.MenuItemId))
        {
            var item = items[group.Key];
            var qty = group.Sum(l => l.Quantity);
            order.AddLine(item.Id, item.Code, item.Name, item.Price, item.Currency, qty);
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }
}
