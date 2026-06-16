using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Accounting.Posting;
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
/// Shared posting logic for a supplier payment. HYBRID accrual model:
/// <list type="bullet">
/// <item>Cash book — keeps a "Purchases" expense <see cref="FinanceTransaction"/> on the payment date so the
/// cash-basis P&amp;L / Cash Book still recognise the cost when cash leaves (unchanged behaviour). This row is
/// NOT mirrored to the GL anymore.</item>
/// <item>GL (accrual) — posts Dr Accounts Payable (2100) / Cr cash-leaf directly, clearing the payable the
/// goods-receipt accrual raised. The receipt already booked the Purchases expense against AP, so the payment
/// only moves AP→cash and the expense is never double-counted in the ledger.</item>
/// </list>
/// Both rows join the caller's unit of work (the caller commits). Used by the standalone payables command and
/// by goods-receipt immediate payment.
/// </summary>
public static class SupplierPaymentPoster
{
    public static async Task AddAsync(
        IAppDbContext db,
        IGeneralLedgerService gl,
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

        var cashAccount = await db.CashAccounts.FirstOrDefaultAsync(a => a.Id == cashAccountId && a.IsActive, cancellationToken)
            ?? throw new ValidationException("The selected cash account does not exist or is inactive.");

        var payment = SupplierPayment.Create(supplierId, cashAccountId, paidOn, amount, reference, notes);
        db.SupplierPayments.Add(payment);

        // Cash-book expense row (cash-basis P&L) — intentionally NOT GL-mirrored.
        var number = await numbers.NextAsync(nowUtc, cancellationToken);
        var txn = FinanceTransaction.Create(
            number, paidOn, TransactionType.Expense, cashAccountId, purchases.Id, amount,
            reference, notes ?? "Supplier payment");
        db.FinanceTransactions.Add(txn);

        // GL accrual: clear the payable against cash (Dr AP / Cr cash-leaf).
        var cashGl = await ChartOfAccountsMapper.EnsureCashAccountGlAsync(db, cashAccount, cancellationToken);
        await gl.PostAsync(db, paidOn, VoucherType.Payment, new[]
        {
            GlPostingLine.Dr(GlCodes.AccountsPayable, amount, $"Supplier payment {reference}"),
            GlPostingLine.CrId(cashGl, amount, $"Paid from {cashAccount.Name}")
        }, reference: $"SUPPAY-{number}", narration: notes ?? "Supplier payment", cancellationToken);

        FinancialAudit.Write(db, FinancialAuditAction.SupplierPaid, currentUser, nameof(SupplierPayment), payment.Id,
            amount: amount, notes: reference);
    }

    /// <summary>The GL leaf the "Purchases" expense category maps to — the account the goods-receipt accrual
    /// debits, so the GL Purchases account is the same one the cash-basis category resolves to.</summary>
    public static async Task<Guid> ResolvePurchasesGlAsync(IAppDbContext db, CancellationToken cancellationToken)
    {
        var purchases = await db.FinanceCategories
            .Where(c => c.Type == TransactionType.Expense && c.IsActive && c.Name == PayablesConstants.PurchasesCategoryName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationException($"No active '{PayablesConstants.PurchasesCategoryName}' expense category found. Create one first.");
        return await ChartOfAccountsMapper.EnsureCategoryGlAsync(db, purchases, cancellationToken);
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
    private readonly IGeneralLedgerService _gl;
    private readonly ITransactionNumberGenerator _numbers;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;

    public RecordSupplierPaymentCommandHandler(
        IAppDbContext db, IGeneralLedgerService gl, ITransactionNumberGenerator numbers, TimeProvider timeProvider, ICurrentUser currentUser)
    {
        _db = db;
        _gl = gl;
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
            _db, _gl, _numbers, _currentUser, nowUtc,
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
