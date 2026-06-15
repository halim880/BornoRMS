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
/// Refunds part or all of a captured payment. Manager/Admin only. A cash refund is paid out of the
/// cashier's open drawer (cash-paid-out bumped). Recomputes the order's payment state.
/// </summary>
public record RefundPaymentCommand(
    Guid OrderId, Guid PaymentId, decimal Amount, string Reason,
    string? ManagerUserName = null, string? ManagerPassword = null) : IRequest<SettlementResultDto>;

public class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("A reason is required to refund a payment.");
    }
}

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, SettlementResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IManagerApprovalService _approvals;
    private readonly IStockConsumptionService _consumption;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IAppDbContext db, ICurrentUser currentUser, IManagerApprovalService approvals,
        IStockConsumptionService consumption, TimeProvider timeProvider, ILogger<RefundPaymentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _approvals = approvals;
        _consumption = consumption;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SettlementResultDto> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
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

        var original = order.Payments.FirstOrDefault(p => p.Id == request.PaymentId)
            ?? throw new NotFoundException("Payment not found.");

        var before = FinancialAudit.Snapshot(order);
        var drawer = await CashDrawerLookup.GetOpenDrawerAsync(_db, _currentUser, cancellationToken);

        try
        {
            // If already imported to the books, reverse the booked takings + reopen for re-accounting
            // (snapshots the pre-refund net, so this must run before RefundPayment mutates the ledger).
            await OrderAccountingReversal.ReverseIfAccountedAsync(_db, order, _timeProvider, cancellationToken);

            order.RefundPayment(request.PaymentId, request.Amount, reason,
                _currentUser.UserId, _currentUser.UserName, drawer?.Id);

            if (drawer is not null && original.Method == PaymentMethod.Cash)
                drawer.RecordCashOut(request.Amount);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        FinancialAudit.Write(_db, FinancialAuditAction.Refunded, _currentUser, nameof(Order), order.Id,
            order.OrderNumber, request.Amount, before, FinancialAudit.Snapshot(order), reason);

        await AddPaymentCommandHandler.SaveWithConcurrencyGuardAsync(_db, cancellationToken);

        // Fully refunded back to unpaid → restore the stock this order consumed (idempotent).
        if (!order.IsPaid && order.AmountPaid <= 0m && order.StockSyncStatus == StockSyncStatus.Synced)
            await OrderStockSync.TryReverseAsync(_db, _consumption, order, _logger, cancellationToken);

        return order.ToSettlementResult();
    }
}
