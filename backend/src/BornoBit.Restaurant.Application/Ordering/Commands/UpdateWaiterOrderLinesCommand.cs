using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

/// <summary>
/// Replaces a running order's lines with the desired set (waiter flow — the page loads the order's
/// items into the cart, the waiter edits, and submits the whole cart back). Lines matching an
/// existing (product, variant) keep their original price snapshot and get the new quantity; new
/// pairs are added at current catalog prices; pairs no longer present are removed.
/// Allowed until the order is paid, completed or cancelled.
/// </summary>
public record UpdateWaiterOrderLinesCommand(
    Guid OrderId,
    IReadOnlyList<PlaceOrderLineInput> Lines) : IRequest<PlaceOrderResult>;

public class UpdateWaiterOrderLinesCommandValidator : AbstractValidator<UpdateWaiterOrderLinesCommand>
{
    public UpdateWaiterOrderLinesCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item is required — cancel the order instead of emptying it.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.MenuItemId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(1);
        });
    }
}

public class UpdateWaiterOrderLinesCommandHandler : IRequestHandler<UpdateWaiterOrderLinesCommand, PlaceOrderResult>
{
    private readonly IAppDbContext _db;

    public UpdateWaiterOrderLinesCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PlaceOrderResult> Handle(UpdateWaiterOrderLinesCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
            throw new ConflictException($"Order {order.OrderNumber} is {order.Status} and cannot be modified.");
        if (order.IsPaid)
            throw new ConflictException($"Order {order.OrderNumber} is already paid.");

        // Desired state, duplicates merged.
        var desired = request.Lines
            .GroupBy(l => (ProductId: l.MenuItemId, l.VariantId))
            .Select(g => (g.Key.ProductId, g.Key.VariantId, Quantity: g.Sum(l => l.Quantity)))
            .ToList();

        // Products are only needed (and re-validated) for NEW lines; quantity changes on existing
        // lines keep their snapshot, so a since-deactivated product doesn't block editing.
        var newPairs = desired
            .Where(d => !order.Lines.Any(l => l.MenuItemId == d.ProductId && l.VariantId == d.VariantId))
            .ToList();

        if (newPairs.Count > 0)
        {
            var productIds = newPairs.Select(d => d.ProductId).Distinct().ToList();
            var products = await _db.Products
                .Include(p => p.Variants)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            foreach (var (productId, variantId, quantity) in newPairs)
            {
                var (name, price, currency, code) = OrderLineResolver.Resolve(products, productId, variantId);
                if (currency != order.Currency)
                    throw new ConflictException($"'{name}' uses {currency}; order is in {order.Currency}.");
                // The order is tracked, so the new line (pre-set Guid key) would be discovered as
                // Modified and saved as an UPDATE — mark it Added explicitly.
                _db.OrderLines.Add(order.AddLine(productId, code, name, price, currency, quantity, variantId));
            }
        }

        // Update quantities of kept lines, remove lines no longer wanted.
        foreach (var line in order.Lines.ToList())
        {
            var match = desired.FirstOrDefault(d => d.ProductId == line.MenuItemId && d.VariantId == line.VariantId);
            if (match.ProductId == Guid.Empty)
                order.RemoveLine(line.Id);
            else if (line.Quantity != match.Quantity)
                order.SetLineQuantity(line.Id, match.Quantity);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }
}
