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

        _db.FinanceTransactions.Remove(txn);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
