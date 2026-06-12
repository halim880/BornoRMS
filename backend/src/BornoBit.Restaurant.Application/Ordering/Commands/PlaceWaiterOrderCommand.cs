using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    IReadOnlyList<PlaceOrderLineInput> Lines) : IRequest<PlaceOrderResult>;

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
    private readonly TimeProvider _timeProvider;

    public PlaceWaiterOrderCommandHandler(IAppDbContext db, IOrderNumberGenerator numbers, TimeProvider timeProvider)
    {
        _db = db;
        _numbers = numbers;
        _timeProvider = timeProvider;
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
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var lineInput in request.Lines)
            OrderLineResolver.Resolve(products, lineInput.MenuItemId, lineInput.VariantId);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var orderNumber = await _numbers.NextAsync(nowUtc, cancellationToken);
        var currency = products[request.Lines[0].MenuItemId].Currency;

        var order = Order.Create(orderNumber, customerId, request.TableId, request.Type, nowUtc, currency, request.Notes);

        // Merge duplicate product+variant pairs into single lines with summed quantity. The Product id
        // is stored in OrderLine.MenuItemId (an unconstrained Guid) alongside a snapshot of code/name/price.
        foreach (var group in request.Lines.GroupBy(l => (l.MenuItemId, l.VariantId)))
        {
            var (name, price, currency2, code) = OrderLineResolver.Resolve(products, group.Key.MenuItemId, group.Key.VariantId);
            var qty = group.Sum(l => l.Quantity);
            order.AddLine(group.Key.MenuItemId, code, name, price, currency2, qty, group.Key.VariantId);
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

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
