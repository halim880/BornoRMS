using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public RefundPaymentCommandHandler(IAppDbContext db, ICurrentUser currentUser, IManagerApprovalService approvals)
    {
        _db = db;
        _currentUser = currentUser;
        _approvals = approvals;
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
        return order.ToSettlementResult();
    }
}
