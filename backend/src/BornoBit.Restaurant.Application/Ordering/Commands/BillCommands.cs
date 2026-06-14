using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Commands;

public record BillSummaryDto(
    decimal Subtotal,
    decimal DiscountAmount,
    decimal GrandTotal,
    bool IsPaid,
    PaymentMethod? Method,
    decimal? Tendered,
    decimal? Change,
    decimal Rounding = 0m);

// ----- Apply discount -----

public record ApplyDiscountCommand(Guid OrderId, decimal? Percent, decimal? Amount, string? Reason)
    : IRequest<BillSummaryDto>;

public class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
{
    public ApplyDiscountCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Percent).InclusiveBetween(0, 100).When(x => x.Percent.HasValue);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue);
    }
}

public class ApplyDiscountCommandHandler : IRequestHandler<ApplyDiscountCommand, BillSummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    public ApplyDiscountCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BillSummaryDto> Handle(ApplyDiscountCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.Include(o => o.Lines).Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order is null) throw new NotFoundException("Order not found.");

        var before = FinancialAudit.Snapshot(order);
        try
        {
            order.ApplyDiscount(request.Percent, request.Amount, request.Reason);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        FinancialAudit.Write(_db, FinancialAuditAction.DiscountApplied, _currentUser, nameof(Order), order.Id,
            order.OrderNumber, order.DiscountAmount, before, FinancialAudit.Snapshot(order), request.Reason);

        await _db.SaveChangesAsync(cancellationToken);
        return ToSummary(order);
    }

    internal static BillSummaryDto ToSummary(Order o) => new(
        o.Subtotal, o.DiscountAmount, o.GrandTotal, o.IsPaid, o.PaymentMethod, o.AmountTendered, o.ChangeGiven, o.RoundingAdjustment);
}

// ----- Record payment -----

public record RecordPaymentCommand(Guid OrderId, PaymentMethod Method, decimal Tendered)
    : IRequest<BillSummaryDto>;

public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Tendered).GreaterThanOrEqualTo(0);
    }
}

public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, BillSummaryDto>
{
    private readonly IAppDbContext _db;
    public RecordPaymentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<BillSummaryDto> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order is null) throw new NotFoundException("Order not found.");

        try
        {
            order.RecordPayment(request.Method, request.Tendered);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApplyDiscountCommandHandler.ToSummary(order);
    }
}
