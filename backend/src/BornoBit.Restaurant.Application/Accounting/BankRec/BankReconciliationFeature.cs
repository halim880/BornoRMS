using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.BankRec;

public record BankReconciliationDto(
    Guid Id, Guid CashAccountId, string CashAccountName, DateTime StatementDate,
    decimal StatementBalance, decimal ClearedBalance, BankReconciliationStatus Status, DateTime? CompletedOn);

public record ReconTransactionDto(
    Guid Id, string Number, DateTime OccurredOn, TransactionType Type, decimal Amount,
    string? Reference, bool IsCleared);

/// <summary>Start a reconciliation for one bank cash account against a statement date + ending balance.</summary>
public record StartBankReconciliationCommand(Guid CashAccountId, DateTime StatementDate, decimal StatementBalance) : IRequest<Guid>;

public class StartBankReconciliationCommandValidator : AbstractValidator<StartBankReconciliationCommand>
{
    public StartBankReconciliationCommandValidator() => RuleFor(x => x.CashAccountId).NotEmpty();
}

public class StartBankReconciliationCommandHandler : IRequestHandler<StartBankReconciliationCommand, Guid>
{
    private readonly IAppDbContext _db;
    public StartBankReconciliationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(StartBankReconciliationCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.CashAccounts.AnyAsync(a => a.Id == request.CashAccountId, cancellationToken))
            throw new NotFoundException("Cash account not found.");

        var recon = BankReconciliation.Create(request.CashAccountId, request.StatementDate, request.StatementBalance);
        _db.BankReconciliations.Add(recon);
        await _db.SaveChangesAsync(cancellationToken);
        return recon.Id;
    }
}

/// <summary>Toggle a transaction's cleared flag and refresh the reconciliation's cleared balance.</summary>
public record ToggleTransactionClearedCommand(Guid ReconciliationId, Guid TransactionId, bool Cleared) : IRequest<decimal>;

public class ToggleTransactionClearedCommandHandler : IRequestHandler<ToggleTransactionClearedCommand, decimal>
{
    private readonly IAppDbContext _db;
    public ToggleTransactionClearedCommandHandler(IAppDbContext db) => _db = db;

    public async Task<decimal> Handle(ToggleTransactionClearedCommand request, CancellationToken cancellationToken)
    {
        var recon = await _db.BankReconciliations.FirstOrDefaultAsync(r => r.Id == request.ReconciliationId, cancellationToken)
            ?? throw new NotFoundException("Reconciliation not found.");
        if (recon.Status == BankReconciliationStatus.Completed)
            throw new ConflictException("This reconciliation is already completed.");

        var txn = await _db.FinanceTransactions.FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        if (request.Cleared) txn.MarkCleared(recon.Id, recon.StatementDate);
        else txn.Unclear();

        var cleared = await BankRecMath.ClearedBalanceAsync(_db, recon.CashAccountId, cancellationToken);
        recon.SetClearedBalance(cleared);
        await _db.SaveChangesAsync(cancellationToken);
        return cleared;
    }
}

/// <summary>Finalise a reconciliation — requires cleared balance to match the statement balance.</summary>
public record CompleteBankReconciliationCommand(Guid ReconciliationId) : IRequest<Unit>;

public class CompleteBankReconciliationCommandHandler : IRequestHandler<CompleteBankReconciliationCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _time;

    public CompleteBankReconciliationCommandHandler(IAppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<Unit> Handle(CompleteBankReconciliationCommand request, CancellationToken cancellationToken)
    {
        var recon = await _db.BankReconciliations.FirstOrDefaultAsync(r => r.Id == request.ReconciliationId, cancellationToken)
            ?? throw new NotFoundException("Reconciliation not found.");

        recon.SetClearedBalance(await BankRecMath.ClearedBalanceAsync(_db, recon.CashAccountId, cancellationToken));
        try { recon.Complete(_time.GetUtcNow().UtcDateTime); }
        catch (InvalidOperationException ex) { throw new ConflictException(ex.Message); }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record GetBankReconciliationsQuery(Guid? CashAccountId = null) : IRequest<IReadOnlyList<BankReconciliationDto>>;

public class GetBankReconciliationsQueryHandler : IRequestHandler<GetBankReconciliationsQuery, IReadOnlyList<BankReconciliationDto>>
{
    private readonly IAppDbContext _db;
    public GetBankReconciliationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BankReconciliationDto>> Handle(GetBankReconciliationsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from r in _db.BankReconciliations
            join a in _db.CashAccounts on r.CashAccountId equals a.Id
            select new { r, AccountName = a.Name };
        if (request.CashAccountId is { } id)
            query = query.Where(x => x.r.CashAccountId == id);

        return await query
            .OrderByDescending(x => x.r.StatementDate)
            .Select(x => new BankReconciliationDto(
                x.r.Id, x.r.CashAccountId, x.AccountName, x.r.StatementDate,
                x.r.StatementBalance, x.r.ClearedBalance, x.r.Status, x.r.CompletedOn))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Transactions for a cash account available to reconcile (with their current cleared flag).</summary>
public record GetReconTransactionsQuery(Guid CashAccountId) : IRequest<IReadOnlyList<ReconTransactionDto>>;

public class GetReconTransactionsQueryHandler : IRequestHandler<GetReconTransactionsQuery, IReadOnlyList<ReconTransactionDto>>
{
    private readonly IAppDbContext _db;
    public GetReconTransactionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReconTransactionDto>> Handle(GetReconTransactionsQuery request, CancellationToken cancellationToken) =>
        await _db.FinanceTransactions
            .Where(t => t.CashAccountId == request.CashAccountId)
            .OrderByDescending(t => t.OccurredOn)
            .Select(t => new ReconTransactionDto(t.Id, t.Number, t.OccurredOn, t.Type, t.Amount, t.Reference, t.IsCleared))
            .ToListAsync(cancellationToken);
}

internal static class BankRecMath
{
    /// <summary>Cleared book balance = account opening + Σ signed cleared transactions (income +, expense −).</summary>
    public static async Task<decimal> ClearedBalanceAsync(IAppDbContext db, Guid cashAccountId, CancellationToken cancellationToken)
    {
        var opening = await db.CashAccounts.Where(a => a.Id == cashAccountId).Select(a => a.OpeningBalance).FirstOrDefaultAsync(cancellationToken);
        var net = await db.FinanceTransactions
            .Where(t => t.CashAccountId == cashAccountId && t.IsCleared)
            .SumAsync(t => (decimal?)(t.Type == TransactionType.Income ? t.Amount : -t.Amount), cancellationToken) ?? 0m;
        return opening + net;
    }
}
