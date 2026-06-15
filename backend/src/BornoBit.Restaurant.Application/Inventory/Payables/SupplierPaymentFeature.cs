using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Payables;

/// <summary>The expense category supplier payments post to (seeded by AccountingSeeder).</summary>
public static class PayablesConstants
{
    public const string PurchasesCategoryName = "Purchases";
}

/// <summary>
/// Shared posting logic: record a payment to a supplier and mirror it as a "Purchases" expense in the
/// cash book, both added to the same unit of work (the caller commits). Used by the standalone payables
/// command and by goods-receipt immediate payment, so cost only ever hits the books once.
/// </summary>
public static class SupplierPaymentPoster
{
    public static async Task AddAsync(
        IAppDbContext db,
        ITransactionNumberGenerator numbers,
        ICurrentUser currentUser,
        DateTime nowUtc,
        Guid supplierId,
        Guid cashAccountId,
        DateTime paidOn,
        decimal amount,
        string? reference,
        string? notes,
        CancellationToken cancellationToken)
    {
        var purchases = await db.FinanceCategories
            .Where(c => c.Type == TransactionType.Expense && c.IsActive && c.Name == PayablesConstants.PurchasesCategoryName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationException($"No active '{PayablesConstants.PurchasesCategoryName}' expense category found. Create one first.");

        if (!await db.CashAccounts.AnyAsync(a => a.Id == cashAccountId && a.IsActive, cancellationToken))
            throw new ValidationException("The selected cash account does not exist or is inactive.");

        var payment = SupplierPayment.Create(supplierId, cashAccountId, paidOn, amount, reference, notes);
        db.SupplierPayments.Add(payment);

        var number = await numbers.NextAsync(nowUtc, cancellationToken);
        var txn = FinanceTransaction.Create(
            number, paidOn, TransactionType.Expense, cashAccountId, purchases.Id, amount,
            reference, notes ?? "Supplier payment");
        db.FinanceTransactions.Add(txn);
        await Accounting.Posting.GeneralLedgerPoster.PostMirrorAsync(db, txn, nowUtc, cancellationToken);

        FinancialAudit.Write(db, FinancialAuditAction.SupplierPaid, currentUser, nameof(SupplierPayment), payment.Id,
            amount: amount, notes: reference);
    }
}

/// <summary>Pay a supplier from a cash account. Books the matching "Purchases" expense atomically.</summary>
public record RecordSupplierPaymentCommand(
    Guid SupplierId,
    Guid CashAccountId,
    DateTime PaidOn,
    decimal Amount,
    string? Reference,
    string? Notes) : IRequest<Guid>;

public class RecordSupplierPaymentCommandValidator : AbstractValidator<RecordSupplierPaymentCommand>
{
    public RecordSupplierPaymentCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(80);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class RecordSupplierPaymentCommandHandler : IRequestHandler<RecordSupplierPaymentCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ITransactionNumberGenerator _numbers;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;

    public RecordSupplierPaymentCommandHandler(
        IAppDbContext db, ITransactionNumberGenerator numbers, TimeProvider timeProvider, ICurrentUser currentUser)
    {
        _db = db;
        _numbers = numbers;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(RecordSupplierPaymentCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await SupplierPaymentPoster.AddAsync(
            _db, _numbers, _currentUser, nowUtc,
            request.SupplierId, request.CashAccountId, request.PaidOn, request.Amount,
            request.Reference, request.Notes, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        // The payment Id isn't returned by the poster; the caller rarely needs it, so return supplier id.
        return request.SupplierId;
    }
}

/// <summary>Accounts payable: per supplier, goods received vs paid, with the balance still owed.</summary>
public record GetPayablesQuery(bool OutstandingOnly = false) : IRequest<IReadOnlyList<PayableDto>>;

public record PayableDto(
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    int PaymentTermsDays,
    decimal Received,
    decimal Paid,
    decimal Outstanding);

public class GetPayablesQueryHandler : IRequestHandler<GetPayablesQuery, IReadOnlyList<PayableDto>>
{
    private readonly IAppDbContext _db;
    public GetPayablesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayableDto>> Handle(GetPayablesQuery request, CancellationToken cancellationToken)
    {
        // Received = value of posted goods receipts per supplier (GoodsReceipt.Subtotal is a C# computed
        // prop EF can't translate, so sum Qty*UnitCost over the lines of posted receipts).
        var received = await (
            from l in _db.GoodsReceiptLines
            join g in _db.GoodsReceipts on l.GoodsReceiptId equals g.Id
            where g.Status == GoodsReceiptStatus.Posted
            group l.Qty * l.UnitCost by g.SupplierId into grp
            select new { SupplierId = grp.Key, Total = grp.Sum() })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Total, cancellationToken);

        var paid = await _db.SupplierPayments
            .GroupBy(p => p.SupplierId)
            .Select(g => new { SupplierId = g.Key, Total = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Total, cancellationToken);

        var suppliers = await _db.Suppliers
            .Select(s => new { s.Id, s.Code, s.Name, s.PaymentTermsDays })
            .ToListAsync(cancellationToken);

        var rows = suppliers
            .Select(s =>
            {
                var rec = received.GetValueOrDefault(s.Id);
                var pay = paid.GetValueOrDefault(s.Id);
                return new PayableDto(s.Id, s.Code, s.Name, s.PaymentTermsDays, rec, pay, rec - pay);
            })
            .Where(r => !request.OutstandingOnly || r.Outstanding > 0m)
            .OrderByDescending(r => r.Outstanding)
            .ThenBy(r => r.SupplierName)
            .ToList();

        return rows;
    }
}

/// <summary>A supplier's payment history (most recent first), optionally for one supplier.</summary>
public record GetSupplierPaymentsQuery(Guid? SupplierId = null) : IRequest<IReadOnlyList<SupplierPaymentDto>>;

public record SupplierPaymentDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    DateTime PaidOn,
    decimal Amount,
    string? CashAccountName,
    string? Reference);

public class GetSupplierPaymentsQueryHandler : IRequestHandler<GetSupplierPaymentsQuery, IReadOnlyList<SupplierPaymentDto>>
{
    private readonly IAppDbContext _db;
    public GetSupplierPaymentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SupplierPaymentDto>> Handle(GetSupplierPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from p in _db.SupplierPayments
            join s in _db.Suppliers on p.SupplierId equals s.Id
            join a in _db.CashAccounts on p.CashAccountId equals a.Id into acc
            from a in acc.DefaultIfEmpty()
            select new { p, SupplierName = s.Name, CashAccountName = a != null ? a.Name : null };

        if (request.SupplierId is { } sid)
            query = query.Where(x => x.p.SupplierId == sid);

        return await query
            .OrderByDescending(x => x.p.PaidOn)
            .Select(x => new SupplierPaymentDto(
                x.p.Id, x.p.SupplierId, x.SupplierName, x.p.PaidOn, x.p.Amount, x.CashAccountName, x.p.Reference))
            .ToListAsync(cancellationToken);
    }
}
