using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Application.Ordering.Printing;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    private readonly IStockConsumptionService _consumption;
    private readonly IKitchenTicketSender _kot;
    private readonly ILogger<UpdateWaiterOrderLinesCommandHandler> _logger;

    public UpdateWaiterOrderLinesCommandHandler(IAppDbContext db, IStockConsumptionService consumption,
        IKitchenTicketSender kot, ILogger<UpdateWaiterOrderLinesCommandHandler> logger)
    {
        _db = db;
        _consumption = consumption;
        _kot = kot;
        _logger = logger;
    }

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

        // If the kitchen already deducted stock (order confirmed) we must keep inventory in step as lines
        // change: deduct added lines, restore removed ones, and net quantity changes. Captured up front
        // because the per-line helpers read the line's current quantity.
        var wasSynced = order.StockSyncStatus == StockSyncStatus.Synced;
        var addedLines = new List<OrderLine>();

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
                addedLines.Add(line);
            }
        }

        // Update quantities of kept lines, remove lines no longer wanted. Keep stock in step when the
        // order was already deducted: restore a removed line, and net a quantity change by reversing the
        // old quantity then consuming the new one (both helpers read line.Quantity).
        foreach (var line in order.Lines.ToList())
        {
            var key = LineOptKey(line);
            var match = desired.FirstOrDefault(d => d.ProductId == line.MenuItemId && d.VariantId == line.VariantId && d.OptKey == key);
            if (match.ProductId == Guid.Empty)
            {
                if (wasSynced) await _consumption.ReverseLineAsync(_db, order, line, cancellationToken);
                order.RemoveLine(line.Id);
            }
            else if (line.Quantity != match.Quantity)
            {
                if (wasSynced) await _consumption.ReverseLineAsync(_db, order, line, cancellationToken);
                order.SetLineQuantity(line.Id, match.Quantity);
                if (wasSynced) await _consumption.ConsumeLineAsync(_db, order, line, cancellationToken);
            }
        }

        // Deduct stock for any newly added lines (modifiers are already attached in-memory).
        if (wasSynced)
            foreach (var line in addedLines)
                await _consumption.ConsumeLineAsync(_db, order, line, cancellationToken);

        // The lines changed after the kitchen fired — re-dispatch an amended KOT so the cooks aren't on a
        // stale ticket. (No-op if the order never printed one.)
        var reprint = order.KotPrintStatus == KotPrintStatus.Printed;
        if (reprint) order.ResetKotForReprint();

        await _db.SaveChangesAsync(cancellationToken);

        if (reprint)
            await OrderKotSync.TryDispatchAsync(_db, _kot, order, _logger, cancellationToken);

        return new PlaceOrderResult(order.Id, order.OrderNumber, order.Total, order.Currency);
    }

    /// <summary>Options key of an existing line (from its snapshotted modifiers), matching <see cref="PlaceOrderCommandHandler.OptionsKey"/>.</summary>
    private static string LineOptKey(OrderLine line)
    {
        var ids = line.Modifiers.Where(m => m.OptionId.HasValue).Select(m => m.OptionId!.Value).Distinct().OrderBy(x => x).ToList();
        return ids.Count == 0 ? "" : string.Join(",", ids);
    }
}
