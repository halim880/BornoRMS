using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Accounting.Posting;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

/// <summary>
/// Imports a day's un-accounted cash-counter takings into the books: one Income ("Sales") transaction per
/// payment method, posted to the cash account whose kind matches the method, then marks every imported
/// invoice as accounted so it can never be imported again. Create + mark commit atomically in one SaveChanges.
/// </summary>
public record ImportCashCounterCommand(DateOnly Date) : IRequest<CashImportResultDto>;

public record CashImportResultDto(int Count, decimal Total, IReadOnlyList<string> SkippedMethods);

public class ImportCashCounterCommandHandler : IRequestHandler<ImportCashCounterCommand, CashImportResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IGeneralLedgerService _gl;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;
    private readonly IBusinessClock _clock;

    public ImportCashCounterCommandHandler(IAppDbContext db, IGeneralLedgerService gl, TimeProvider timeProvider, ICurrentUser currentUser, IBusinessClock clock)
    {
        _db = db;
        _gl = gl;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CashImportResultDto> Handle(ImportCashCounterCommand request, CancellationToken cancellationToken)
    {
        var date = request.Date;
        var (start, end) = _clock.DayWindowUtc(date);

        // Tracked (mutated below) so MarkAccounted persists.
        var orders = await _db.Orders
            .Where(o => o.IsPaid
                        && o.Status != OrderStatus.Cancelled
                        && o.AccountedAtUtc == null
                        && o.PaidAtUtc != null
                        && o.PaidAtUtc >= start
                        && o.PaidAtUtc < end)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
            return new CashImportResultDto(0, 0m, Array.Empty<string>());

        var orderIds = orders.Select(o => o.Id).ToList();

        // Net captured takings per method, sourced from the payment ledger (reflects split/partial + refunds).
        var byMethod = await _db.Payments
            .Where(p => orderIds.Contains(p.OrderId) && p.Status == PaymentEntryStatus.Captured)
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount) })
            .ToListAsync(cancellationToken);

        var salesCategory = await _db.FinanceCategories
            .Where(c => c.Type == TransactionType.Income && c.IsActive)
            .OrderByDescending(c => c.Name == "Sales")
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active income category found. Create a 'Sales' category first.");

        var accounts = await _db.CashAccounts
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var occurredOn = date.ToDateTime(TimeOnly.MinValue);

        // Inline numbering (mirrors TransactionNumberGenerator) — one SaveChanges, so count once and increment.
        var prefix = $"TXN-{nowUtc.Date:yyyyMMdd}-";
        var seq = await _db.FinanceTransactions
            .IgnoreQueryFilters()
            .CountAsync(t => t.Number.StartsWith(prefix), cancellationToken);

        var skipped = new List<string>();
        var postedTotal = 0m;
        var posted = false;

        foreach (var group in byMethod.OrderBy(g => g.Method))
        {
            var method = group.Method;
            var amount = group.Amount;
            if (amount <= 0m) continue;

            var account = accounts.FirstOrDefault(a => a.Kind == MapKind(method));
            if (account is null)
            {
                skipped.Add(method.ToString());
                continue; // leave these invoices un-accounted so a later import can pick them up
            }

            var number = $"{prefix}{++seq:D4}";
            var txn = FinanceTransaction.Create(
                number, occurredOn, TransactionType.Income, account.Id, salesCategory.Id,
                amount, $"CashCounter {date:yyyy-MM-dd}", $"Imported {method} takings from Cash Counter");
            _db.FinanceTransactions.Add(txn);
            await Posting.GeneralLedgerPoster.PostMirrorAsync(_db, txn, nowUtc, cancellationToken);

            postedTotal += amount;
            posted = true;
        }

        // If every method posted (nothing skipped), the orders are fully accounted.
        if (posted && skipped.Count == 0)
        {
            foreach (var order in orders)
                order.MarkAccounted();

            // VAT accrual (hybrid): the cash mirror booked the FULL takings (VAT-inclusive) to Sales income.
            // Reclass the output-VAT portion out of Sales into the VAT Payable liability (2200) so the balance
            // sheet shows what is owed to NBR. Single correcting entry: Dr Sales-income / Cr VAT Payable.
            var vat = await _db.OrderLines
                .Where(l => orderIds.Contains(l.OrderId))
                .SumAsync(l => (decimal?)l.TaxAmountSnapshot, cancellationToken) ?? 0m;

            if (vat > 0m)
            {
                var salesGl = await ChartOfAccountsMapper.EnsureCategoryGlAsync(_db, salesCategory, cancellationToken);
                await _gl.PostAsync(_db, occurredOn, VoucherType.Journal, new[]
                {
                    GlPostingLine.DrId(salesGl, vat, "Output VAT reclassified out of sales"),
                    GlPostingLine.Cr(GlCodes.VatPayable, vat, $"Output VAT on {date:yyyy-MM-dd} takings")
                }, reference: $"VAT-{date:yyyyMMdd}", narration: $"Output VAT accrual {date:yyyy-MM-dd}", cancellationToken);
            }
        }

        if (posted)
        {
            FinancialAudit.Write(_db, FinancialAuditAction.CashImported, _currentUser, nameof(FinanceTransaction), Guid.Empty,
                amount: postedTotal, notes: $"Imported {date:yyyy-MM-dd} cash-counter takings ({orders.Count} order(s))");
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new CashImportResultDto(orders.Count, postedTotal, skipped);
    }

    private static CashAccountKind MapKind(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => CashAccountKind.Cash,
        PaymentMethod.Mobile => CashAccountKind.MobileWallet,
        PaymentMethod.Card => CashAccountKind.Bank,
        _ => CashAccountKind.Cash
    };
}
