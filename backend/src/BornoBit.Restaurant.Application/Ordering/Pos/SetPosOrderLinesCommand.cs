using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Pos;

/// <summary>
/// Replaces a POS order's lines with the desired set. Same merge semantics as
/// <see cref="UpdateWaiterOrderLinesCommand"/> (kept lines preserve their price snapshot, new
/// pairs are added at current catalog prices, missing pairs are removed) but an EMPTY set is
/// allowed: POS orders start with zero lines and the cashier may remove the last item again.
/// Allowed until the order is paid, completed or cancelled.
/// </summary>
public record SetPosOrderLinesCommand(
    Guid OrderId,
    IReadOnlyList<PlaceOrderLineInput> Lines) : IRequest<PlaceOrderResult>;

public class SetPosOrderLinesCommandValidator : AbstractValidator<SetPosOrderLinesCommand>
{
    public SetPosOrderLinesCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.MenuItemId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(1);
        });
    }
}

public class SetPosOrderLinesCommandHandler : IRequestHandler<SetPosOrderLinesCommand, PlaceOrderResult>
{
    private readonly IAppDbContext _db;

    public SetPosOrderLinesCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PlaceOrderResult> Handle(SetPosOrderLinesCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
            throw new ConflictException($"Order {order.OrderNumber} is {order.Status} and cannot be modified.");
        if (order.IsPaid)
            throw new ConflictException($"Order {order.OrderNumber} is already paid.");

        // Desired state, duplicates merged. The grouping key includes the chosen add-on options, so two
        // lines of the same product+variant with different modifiers stay distinct.
        var desired = request.Lines
            .GroupBy(l => (ProductId: l.MenuItemId, l.VariantId, OptKey: PlaceOrderCommandHandler.OptionsKey(l)))
            .Select(g => (g.Key.ProductId, g.Key.VariantId, g.Key.OptKey,
                Quantity: g.Sum(l => l.Quantity), OptionIds: g.First().OptionIds))
            .ToList();

        // Products are only needed (and re-validated) for NEW lines; quantity changes on existing
        // lines keep their snapshot, so a since-deactivated product doesn't block editing.
        var newPairs = desired
            .Where(d => !order.Lines.Any(l => l.MenuItemId == d.ProductId && l.VariantId == d.VariantId && LineOptKey(l) == d.OptKey))
            .ToList();

        if (newPairs.Count > 0)
        {
            var productIds = newPairs.Select(d => d.ProductId).Distinct().ToList();
            var products = await _db.Products
                .Include(p => p.Variants)
                .Include(p => p.OptionGroups).ThenInclude(g => g.Options)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var stationNames = await _db.KitchenStations.ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

            foreach (var (productId, variantId, _, quantity, optionIds) in newPairs)
            {
                var (name, price, currency, code, stationId, prepMinutes) = OrderLineResolver.Resolve(products, productId, variantId);
                if (currency != order.Currency)
                    throw new ConflictException($"'{name}' uses {currency}; order is in {order.Currency}.");
                var stationName = stationId is { } sid && stationNames.TryGetValue(sid, out var sn) ? sn : null;
                // The order is tracked, so the new line (pre-set Guid key) would be discovered as
                // Modified and saved as an UPDATE — mark it Added explicitly.
                var line = order.AddLine(productId, code, name, price, currency, quantity, variantId, stationId, stationName, null, prepMinutes);
                _db.OrderLines.Add(line);
                foreach (var m in OrderLineResolver.ResolveModifiers(products, productId, optionIds))
                    _db.OrderLineModifiers.Add(line.AddModifier(m.OptionId, m.GroupName, m.OptionName, m.PriceDelta));
            }
        }

        // Update quantities of kept lines, remove lines no longer wanted.
        foreach (var line in order.Lines.ToList())
        {
            var key = LineOptKey(line);
            var match = desired.FirstOrDefault(d => d.ProductId == line.MenuItemId && d.VariantId == line.VariantId && d.OptKey == key);
            if (match.ProductId == Guid.Empty)
                order.RemoveLine(line.Id);
            else if (line.Quantity != match.Quantity)
                order.SetLineQuantity(line.Id, match.Quantity);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }

    /// <summary>Options key of an existing line (from its snapshotted modifiers), matching <see cref="PlaceOrderCommandHandler.OptionsKey"/>.</summary>
    private static string LineOptKey(OrderLine line)
    {
        var ids = line.Modifiers.Where(m => m.OptionId.HasValue).Select(m => m.OptionId!.Value).Distinct().OrderBy(x => x).ToList();
        return ids.Count == 0 ? "" : string.Join(",", ids);
    }
}
