using BornoBit.Restaurant.Application.Accounting.Posting;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

/// <summary>
/// Keeps the books consistent when a paid order is refunded/voided <em>after</em> its takings were already
/// imported into the accounts (<see cref="ImportCashCounterCommand"/> stamped <c>Order.AccountedAtUtc</c>).
/// Posts a reversing <c>Expense</c> ("Refunds") for the order's already-booked net per payment method and
/// reopens the invoice (<see cref="Order.ClearAccounted"/>) so the next cash-counter import re-books the
/// corrected net. A not-yet-accounted order is a no-op — the import already nets in-window refunds itself.
/// </summary>
public static class OrderAccountingReversal
{
    /// <summary>The cash account kind a tender of this method is booked under (mirrors the cash-counter import).</summary>
    public static CashAccountKind MapKind(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => CashAccountKind.Cash,
        PaymentMethod.Mobile => CashAccountKind.MobileWallet,
        PaymentMethod.Card => CashAccountKind.Bank,
        _ => CashAccountKind.Cash
    };

    /// <summary>
    /// Call BEFORE applying the refund/void so the reversed net reflects what was originally booked.
    /// Does not call SaveChanges — the caller's single save commits the reversal + the refund/void atomically.
    /// </summary>
    public static async Task ReverseIfAccountedAsync(IAppDbContext db, IGeneralLedgerService gl, Order order, TimeProvider time, CancellationToken ct)
    {
        if (order.AccountedAtUtc is null) return; // never imported → import nets the refund on its own

        // Net captured per method (charge − refund) — the amount the cash-counter import booked for this order.
        var byMethod = order.Payments
            .Where(p => p.Status == PaymentEntryStatus.Captured)
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount) })
            .Where(x => x.Amount > 0m)
            .ToList();

        if (byMethod.Count > 0)
        {
            var refundsCategory = await GetOrCreateRefundsCategoryAsync(db, ct);
            var accounts = await db.CashAccounts.Where(a => a.IsActive).ToListAsync(ct);

            var nowUtc = time.GetUtcNow().UtcDateTime;
            var prefix = $"TXN-{nowUtc.Date:yyyyMMdd}-";
            // Inline numbering (mirrors ImportCashCounterCommand): no SaveChanges between rows, so count once.
            var seq = await db.FinanceTransactions.IgnoreQueryFilters().CountAsync(t => t.Number.StartsWith(prefix), ct);

            foreach (var group in byMethod)
            {
                var account = accounts.FirstOrDefault(a => a.Kind == MapKind(group.Method));
                if (account is null) continue; // booked to no account → nothing to reverse for this method

                var number = $"{prefix}{++seq:D4}";
                var txn = FinanceTransaction.Create(
                    number, nowUtc, TransactionType.Expense, account.Id, refundsCategory.Id, group.Amount,
                    $"Order {order.OrderNumber}", $"Reversal of accounted {group.Method} takings (refund/void)");
                db.FinanceTransactions.Add(txn);
                await Posting.GeneralLedgerPoster.PostMirrorAsync(db, txn, nowUtc, ct);
            }
        }

        // Reverse the output-VAT accrual booked for this order at import (Dr VAT Payable / Cr Sales-income).
        // The order is being reopened; the next import re-accrues VAT from the (still-frozen) line snapshots.
        var vat = await db.OrderLines
            .Where(l => l.OrderId == order.Id)
            .SumAsync(l => (decimal?)l.TaxAmountSnapshot, ct) ?? 0m;
        if (vat > 0m)
        {
            var salesCategory = await db.FinanceCategories
                .Where(c => c.Type == TransactionType.Income && c.IsActive)
                .OrderByDescending(c => c.Name == "Sales")
                .FirstOrDefaultAsync(ct);
            if (salesCategory is not null)
            {
                var salesGl = await ChartOfAccountsMapper.EnsureCategoryGlAsync(db, salesCategory, ct);
                var nowUtc2 = time.GetUtcNow().UtcDateTime;
                await gl.PostAsync(db, nowUtc2, VoucherType.Journal, new[]
                {
                    GlPostingLine.Dr(GlCodes.VatPayable, vat, $"Reverse VAT on {order.OrderNumber} (refund/void)"),
                    GlPostingLine.CrId(salesGl, vat, "Reverse output VAT into sales")
                }, reference: $"VAT-REV-{order.OrderNumber}", narration: $"Reverse output VAT for {order.OrderNumber}", ct);
            }
        }

        // Reopen for re-accounting: the next import re-books the corrected net (or nothing once fully unpaid).
        order.ClearAccounted();
    }

    private static async Task<FinanceCategory> GetOrCreateRefundsCategoryAsync(IAppDbContext db, CancellationToken ct)
    {
        var existing = await db.FinanceCategories
            .FirstOrDefaultAsync(c => c.Type == TransactionType.Expense && c.Name == "Refunds", ct);
        if (existing is not null) return existing;

        var created = FinanceCategory.Create("Refunds", TransactionType.Expense);
        db.FinanceCategories.Add(created);
        return created;
    }
}
