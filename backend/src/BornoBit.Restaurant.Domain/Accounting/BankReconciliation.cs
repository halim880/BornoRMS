using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Accounting;

public enum BankReconciliationStatus { InProgress = 1, Completed = 2 }

/// <summary>
/// A bank-statement reconciliation for one cash account (kind Bank). Transactions are marked cleared as they
/// appear on the statement; the reconciliation completes once the cleared balance equals the statement balance.
/// </summary>
public class BankReconciliation : AuditableEntity
{
    public Guid CashAccountId { get; private set; }
    public DateTime StatementDate { get; private set; }
    public decimal StatementBalance { get; private set; }
    public decimal ClearedBalance { get; private set; }
    public BankReconciliationStatus Status { get; private set; } = BankReconciliationStatus.InProgress;
    public DateTime? CompletedOn { get; private set; }

    private BankReconciliation() { }

    public static BankReconciliation Create(Guid cashAccountId, DateTime statementDate, decimal statementBalance)
    {
        if (cashAccountId == Guid.Empty) throw new ArgumentException("Cash account is required.", nameof(cashAccountId));
        return new BankReconciliation
        {
            CashAccountId = cashAccountId,
            StatementDate = statementDate.Date,
            StatementBalance = statementBalance,
            Status = BankReconciliationStatus.InProgress
        };
    }

    public void SetClearedBalance(decimal clearedBalance) => ClearedBalance = clearedBalance;

    public void Complete(DateTime completedOn)
    {
        if (Status == BankReconciliationStatus.Completed) throw new InvalidOperationException("Reconciliation is already completed.");
        if (Math.Abs(ClearedBalance - StatementBalance) > 0.01m)
            throw new InvalidOperationException($"Cleared balance {ClearedBalance:0.00} does not match statement balance {StatementBalance:0.00}.");
        Status = BankReconciliationStatus.Completed;
        CompletedOn = completedOn.Date;
    }
}
