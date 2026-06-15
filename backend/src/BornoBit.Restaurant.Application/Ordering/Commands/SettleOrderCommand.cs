using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Application.Ordering.Common;
using BornoBit.Restaurant.Application.Ordering.Payments;
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
    private readonly ILogger<SettleOrderCommandHandler> _logger;

    public SettleOrderCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, IStockConsumptionService consumption,
        IPaymentGateway gateway, IDineInSessionResolver sessions, ILogger<SettleOrderCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _consumption = consumption;
        _gateway = gateway;
        _sessions = sessions;
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

        var subtotal = order.Subtotal;

        // Resolve the effective discount to derive the taxable base and the high-discount gate.
        decimal discountAmt = request.DiscountPercent is { } pct
            ? Math.Round(subtotal * pct / 100m, 2)
            : request.DiscountAmount ?? 0m;
        decimal effectivePercent = subtotal > 0m ? Math.Round(discountAmt / subtotal * 100m, 2) : 0m;

        if (effectivePercent >= highDiscountThreshold && highDiscountThreshold > 0m)
            PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager);

        var taxableBase = Math.Max(0m, subtotal - discountAmt);
        var taxAmount = request.TaxAmount ?? Math.Round(taxableBase * vatPercent / 100m, 2);
        var serviceAmount = request.ServiceChargeAmount ?? Math.Round(taxableBase * servicePercent / 100m, 2);
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

        FinancialAudit.Write(_db, FinancialAuditAction.OrderSettled, _currentUser, nameof(Order), order.Id,
            order.OrderNumber, order.GrandTotal, before, FinancialAudit.Snapshot(order));

        await AddPaymentCommandHandler.SaveWithConcurrencyGuardAsync(_db, cancellationToken);

        // POS settle-and-pay completes an order without a separate Confirm — deduct stock here too
        // (no-op via idempotency guard if it was already deducted on Confirm/BeginPreparing).
        if (order.IsPaid && order.StockSyncStatus is not (StockSyncStatus.Synced or StockSyncStatus.Reversed))
            await OrderStockSync.TryApplyAsync(_db, _consumption, order, _logger, cancellationToken);

        // Free the table once this was the session's last unpaid order.
        if (order.IsPaid && order.DiningSessionId is { } sessionId)
        {
            await _sessions.CloseIfEmptyAsync(_db, sessionId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var change = order.Payments.OrderBy(p => p.CreatedAtUtc).LastOrDefault()?.Change ?? 0m;
        return order.ToSettlementResult(change);
    }
}
