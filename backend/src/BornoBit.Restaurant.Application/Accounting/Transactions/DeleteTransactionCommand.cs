using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

/// <summary>Soft-deletes a transaction (the global query filter hides it thereafter).</summary>
public record DeleteTransactionCommand(Guid Id) : IRequest;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly IAppDbContext _db;

    public DeleteTransactionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var txn = await _db.FinanceTransactions.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        // Reverse the GL: void the mirror journal(s) so the ledger no longer counts this transaction.
        await Posting.GeneralLedgerPoster.VoidMirrorsAsync(_db, txn.Number, cancellationToken);

        _db.FinanceTransactions.Remove(txn);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
