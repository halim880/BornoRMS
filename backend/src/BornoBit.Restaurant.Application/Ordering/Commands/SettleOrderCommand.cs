using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Application.Ordering.Common;
using BornoBit.Restaurant.Application.Ordering.Payments;
using BornoBit.Restaurant.Application.Ordering.Printing;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

/// <summary>
/// The settlement engine: applies discount + VAT + service charge + tip + rounding (VAT/service default
/// from <see cref="Domain.Settings.RestaurantBillingSettings"/> when not overridden), then records the
/// tender payments (supports split) — all in one atomic transaction. Cashier+ may settle; a discount at
/// or above the configured high-discount threshold requires Manager/Admin.
/// </summary>
public record SettleOrderCommand(
    Guid OrderId,
    decimal? DiscountPercent,
    decimal? DiscountAmount,
    string? DiscountReason,
    decimal? TaxAmount,
    decimal? ServiceChargeAmount,
    decimal? TipAmount,
    decimal? Rounding,
    IReadOnlyList<PaymentEntryInput> Payments) : IRequest<SettlementResultDto>;

public class SettleOrderCommandValidator : AbstractValidator<SettleOrderCommand>
{
    public SettleOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100).When(x => x.DiscountPercent.HasValue);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).When(x => x.TaxAmount.HasValue);
        RuleFor(x => x.ServiceChargeAmount).GreaterThanOrEqualTo(0).When(x => x.ServiceChargeAmount.HasValue);
        RuleFor(x => x.TipAmount).GreaterThanOrEqualTo(0).When(x => x.TipAmount.HasValue);
        RuleForEach(x => x.Payments).ChildRules(p =>
        {
            p.RuleFor(x => x.Amount).GreaterThan(0);
            p.RuleFor(x => x.Tendered).GreaterThanOrEqualTo(0);
        });
    }
}

public class SettleOrderCommandHandler : IRequestHandler<SettleOrderCommand, SettlementResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStockConsumptionService _consumption;
    private readonly IPaymentGateway _gateway;
    private readonly IDineInSessionResolver _sessions;
    private readonly IKitchenTicketSender _kot;
    private readonly ILogger<SettleOrderCommandHandler> _logger;

    public SettleOrderCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, IStockConsumptionService consumption,
        IPaymentGateway gateway, IDineInSessionResolver sessions, IKitchenTicketSender kot,
        ILogger<SettleOrderCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _consumption = consumption;
        _gateway = gateway;
        _sessions = sessions;
        _kot = kot;
        _logger = logger;
    }

    public async Task<SettlementResultDto> Handle(SettleOrderCommand request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager, Roles.Cashier);

        var order = await _db.Orders
            .Include(o => o.Lines)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        var settings = await _db.RestaurantBillingSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var vatPercent = settings?.VatPercent ?? 0m;
        var servicePercent = settings?.ServiceChargePercent ?? 0m;
        var highDiscountThreshold = settings?.HighDiscountThresholdPercent ?? 100m;
        var priceIncludesTax = settings?.PriceIncludesTax ?? false;

        var subtotal = order.Subtotal;

        // Resolve the effective discount to derive the taxable base and the high-discount gate.
        decimal discountAmt = request.DiscountPercent is { } pct
            ? Math.Round(subtotal * pct / 100m, 2)
            : request.DiscountAmount ?? 0m;
        decimal effectivePercent = subtotal > 0m ? Math.Round(discountAmt / subtotal * 100m, 2) : 0m;

        if (effectivePercent >= highDiscountThreshold && highDiscountThreshold > 0m)
            PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager);

        var taxableBase = Math.Max(0m, subtotal - discountAmt);

        // Per-line VAT: rate comes from each line's product category (null → restaurant default VAT).
        // Order-level discount is allocated proportionally across lines. Snapshots are stamped on the
        // lines (frozen for the VAT report). With price-inclusive VAT the tax is extracted from the line
        // total and NOT added to the payable; otherwise it is added on top (the legacy behaviour).
        var rateByProduct = await BuildRateMapAsync(order, vatPercent, cancellationToken);
        var computedTax = ComputeLineTaxes(order, rateByProduct, vatPercent, priceIncludesTax, subtotal, discountAmt);

        var serviceAmount = request.ServiceChargeAmount ?? Math.Round(taxableBase * servicePercent / 100m, 2);
        var taxAmount = request.TaxAmount ?? (priceIncludesTax ? 0m : computedTax);
        var tip = request.TipAmount ?? 0m;
        var rounding = request.Rounding ?? 0m;

        var before = FinancialAudit.Snapshot(order);
        var drawer = await CashDrawerLookup.GetOpenDrawerAsync(_db, _currentUser, cancellationToken);

        try
        {
            order.Settle(request.DiscountPercent, request.DiscountAmount, request.DiscountReason,
                taxAmount, serviceAmount, tip, rounding);

            foreach (var input in request.Payments)
            {
                var tender = await _gateway.AuthorizeIfNonCashAsync(input, order.OrderNumber, cancellationToken);
                var payment = order.AddPayment(tender.Method, tender.Provider, tender.Amount, tender.Tendered,
                    _currentUser.UserId, _currentUser.UserName, drawer?.Id, tender.Reference);

                if (drawer is not null && tender.Method == PaymentMethod.Cash)
                    drawer.RecordCashIn(payment.Amount);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        // Petpooja QSR: paying fires the kitchen — payment must never bypass the kitchen. A still-Placed
        // order is confirmed (and its KOT dispatched below) instead of being left paid-but-never-cooked.
        var routedToKitchen = false;
        if (order.IsPaid && order.Status == OrderStatus.Placed) { order.Confirm(); routedToKitchen = true; }

        // Completed = Served AND Paid: paying an already-served order completes it. Payment alone does not.
        if (order.Status == OrderStatus.Served && order.IsPaid) order.Complete();

        FinancialAudit.Write(_db, FinancialAuditAction.OrderSettled, _currentUser, nameof(Order), order.Id,
            order.OrderNumber, order.GrandTotal, before, FinancialAudit.Snapshot(order));

        await AddPaymentCommandHandler.SaveWithConcurrencyGuardAsync(_db, cancellationToken);

        // POS settle-and-pay completes an order without a separate Confirm — deduct stock here too
        // (no-op via idempotency guard if it was already deducted on Confirm/BeginPreparing).
        if (order.IsPaid && order.StockSyncStatus is not (StockSyncStatus.Synced or StockSyncStatus.Reversed))
            await OrderStockSync.TryApplyAsync(_db, _consumption, order, _logger, cancellationToken);

        // Settle routed the order to the kitchen → dispatch its ticket (idempotent + failure-tolerant).
        if (routedToKitchen)
            await OrderKotSync.TryDispatchAsync(_db, _kot, order, _logger, cancellationToken);

        // Free the table once this was the session's last unpaid order.
        if (order.IsPaid && order.DiningSessionId is { } sessionId)
        {
            await _sessions.CloseIfEmptyAsync(_db, sessionId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Once paid, stock is (best-effort) deducted — warn the cashier about any line that has no SKU/recipe.
        var warnings = order.IsPaid
            ? await StockTrackingInspector.FindUntrackedAsync(_db, order, cancellationToken)
            : Array.Empty<string>();

        var change = order.Payments.OrderBy(p => p.CreatedAtUtc).LastOrDefault()?.Change ?? 0m;
        return order.ToSettlementResult(change, warnings);
    }

    /// <summary>Map each ordered product id to its category VAT rate (null where the category inherits the default).</summary>
    private async Task<Dictionary<Guid, decimal?>> BuildRateMapAsync(Order order, decimal vatPercent, CancellationToken cancellationToken)
    {
        var productIds = order.Lines.Select(l => l.MenuItemId).Distinct().ToList();
        return await (
            from p in _db.Products
            join c in _db.ProductCategories on p.ProductCategoryId equals c.Id
            where productIds.Contains(p.Id)
            select new { p.Id, c.TaxRatePercent })
            .ToDictionaryAsync(x => x.Id, x => x.TaxRatePercent, cancellationToken);
    }

    /// <summary>
    /// Stamps the VAT snapshot on every line and returns the total VAT. Each line's taxable base is its
    /// share of the post-discount subtotal; <paramref name="priceIncludesTax"/> selects extract-from vs
    /// add-on VAT. Snapshots always store the VAT-exclusive (net) base so the VAT report reconciles.
    /// </summary>
    private static decimal ComputeLineTaxes(
        Order order, IReadOnlyDictionary<Guid, decimal?> rateByProduct,
        decimal vatPercent, bool priceIncludesTax, decimal subtotal, decimal discountAmt)
    {
        var factor = subtotal > 0m ? (subtotal - discountAmt) / subtotal : 0m;
        var total = 0m;

        foreach (var line in order.Lines)
        {
            var rate = (rateByProduct.TryGetValue(line.MenuItemId, out var r) ? r : null) ?? vatPercent;
            var gross = Math.Round(line.LineTotal * factor, 2);

            decimal net, tax;
            if (priceIncludesTax)
            {
                tax = rate > 0m ? Math.Round(gross * rate / (100m + rate), 2) : 0m;
                net = gross - tax;
            }
            else
            {
                net = gross;
                tax = Math.Round(net * rate / 100m, 2);
            }

            line.TaxRatePercentSnapshot = rate;
            line.TaxableAmountSnapshot = net;
            line.TaxAmountSnapshot = tax;
            total += tax;
        }

        return total;
    }
}
