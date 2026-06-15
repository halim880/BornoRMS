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
/// Records one or more tenders against an order — the engine behind partial and split payments
/// (e.g. 300 cash + 200 bKash). Cashier+ only. Each cash tender is attributed to the cashier's open
/// drawer (cash-received bumped). Returns the updated settlement state.
/// </summary>
public record AddPaymentCommand(Guid OrderId, IReadOnlyList<PaymentEntryInput> Payments)
    : IRequest<SettlementResultDto>;

public class AddPaymentCommandValidator : AbstractValidator<AddPaymentCommand>
{
    public AddPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Payments).NotEmpty().WithMessage("At least one payment is required.");
        RuleForEach(x => x.Payments).ChildRules(p =>
        {
            p.RuleFor(x => x.Amount).GreaterThan(0);
            p.RuleFor(x => x.Tendered).GreaterThanOrEqualTo(0);
        });
    }
}

public class AddPaymentCommandHandler : IRequestHandler<AddPaymentCommand, SettlementResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStockConsumptionService _consumption;
    private readonly IPaymentGateway _gateway;
    private readonly IDineInSessionResolver _sessions;
    private readonly ILogger<AddPaymentCommandHandler> _logger;

    public AddPaymentCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, IStockConsumptionService consumption,
        IPaymentGateway gateway, IDineInSessionResolver sessions, ILogger<AddPaymentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _consumption = consumption;
        _gateway = gateway;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<SettlementResultDto> Handle(AddPaymentCommand request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager, Roles.Cashier);

        var order = await _db.Orders
            .Include(o => o.Lines)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        var drawer = await CashDrawerLookup.GetOpenDrawerAsync(_db, _currentUser, cancellationToken);
        var before = FinancialAudit.Snapshot(order);
        decimal captured = 0m;

        try
        {
            foreach (var input in request.Payments)
            {
                var tender = await _gateway.AuthorizeIfNonCashAsync(input, order.OrderNumber, cancellationToken);
                var payment = order.AddPayment(tender.Method, tender.Provider, tender.Amount, tender.Tendered,
                    _currentUser.UserId, _currentUser.UserName, drawer?.Id, tender.Reference);
                captured += payment.Amount;

                if (drawer is not null && tender.Method == PaymentMethod.Cash)
                    drawer.RecordCashIn(payment.Amount);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        FinancialAudit.Write(_db, FinancialAuditAction.PaymentCaptured, _currentUser, nameof(Order), order.Id,
            order.OrderNumber, captured, before, FinancialAudit.Snapshot(order));

        await SaveWithConcurrencyGuardAsync(_db, cancellationToken);

        // POS quick-pay completes an order without a separate Confirm step — deduct stock here too.
        // Idempotency guard makes this a no-op if the order was already deducted on Confirm/BeginPreparing.
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

    internal static async Task SaveWithConcurrencyGuardAsync(IAppDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This order was changed by someone else. Reload and try again.");
        }
    }
}

/// <summary>Shared lookup for the current cashier's open drawer (null if none is open).</summary>
public static class CashDrawerLookup
{
    public static async Task<CashDrawerSession?> GetOpenDrawerAsync(
        IAppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId) return null;
        return await db.CashDrawerSessions
            .FirstOrDefaultAsync(d => d.Status == DrawerStatus.Open && d.CashierUserId == userId, cancellationToken);
    }
}
