using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Accounting.Transactions;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Application.Inventory.Consumption;
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
/// Voids a captured (mistaken) payment. Manager/Admin only. Reverses the cashier's drawer cash-in
/// for a cash tender. Recomputes the order's payment state.
/// </summary>
public record VoidPaymentCommand(
    Guid OrderId, Guid PaymentId, string Reason,
    string? ManagerUserName = null, string? ManagerPassword = null) : IRequest<SettlementResultDto>;

public class VoidPaymentCommandValidator : AbstractValidator<VoidPaymentCommand>
{
    public VoidPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("A reason is required to void a payment.");
    }
}

public class VoidPaymentCommandHandler : IRequestHandler<VoidPaymentCommand, SettlementResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IManagerApprovalService _approvals;
    private readonly IStockConsumptionService _consumption;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VoidPaymentCommandHandler> _logger;

    public VoidPaymentCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, IManagerApprovalService approvals,
        IStockConsumptionService consumption, TimeProvider timeProvider, ILogger<VoidPaymentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _approvals = approvals;
        _consumption = consumption;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SettlementResultDto> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        // Instant-override: a manager role on the till proceeds; otherwise a manager credential authorizes it.
        var approver = await ManagerApprovalResolver.ResolveApproverAsync(
            _currentUser, _approvals, request.ManagerUserName, request.ManagerPassword, cancellationToken);
        var reason = ManagerApprovalResolver.WithApprover(request.Reason, approver);

        var order = await _db.Orders
            .Include(o => o.Lines)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        var payment = order.Payments.FirstOrDefault(p => p.Id == request.PaymentId)
            ?? throw new NotFoundException("Payment not found.");

        var before = FinancialAudit.Snapshot(order);
        var wasCashDrawer = payment is { Method: PaymentMethod.Cash, CashDrawerSessionId: not null };
        var drawerId = payment.CashDrawerSessionId;
        var amount = payment.Amount;

        try
        {
            // If already imported to the books, reverse the booked takings + reopen for re-accounting
            // (snapshots the pre-void net, so this must run before VoidPayment mutates the ledger).
            await OrderAccountingReversal.ReverseIfAccountedAsync(_db, order, _timeProvider, cancellationToken);

            order.VoidPayment(request.PaymentId, reason);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        if (wasCashDrawer && drawerId is { } id)
        {
            var drawer = await _db.CashDrawerSessions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (drawer is { Status: DrawerStatus.Open }) drawer.RecordCashOut(amount);
        }

        FinancialAudit.Write(_db, FinancialAuditAction.PaymentVoided, _currentUser, nameof(Order), order.Id,
            order.OrderNumber, amount, before, FinancialAudit.Snapshot(order), reason);

        await AddPaymentCommandHandler.SaveWithConcurrencyGuardAsync(_db, cancellationToken);

        // Voiding the last charge drops the order back to unpaid → restore the stock it consumed (idempotent).
        if (!order.IsPaid && order.AmountPaid <= 0m && order.StockSyncStatus == StockSyncStatus.Synced)
            await OrderStockSync.TryReverseAsync(_db, _consumption, order, _logger, cancellationToken);

        return order.ToSettlementResult();
    }
}
