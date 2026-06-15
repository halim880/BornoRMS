using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

public record UpdateTransactionCommand(
    Guid Id,
    DateTime OccurredOn,
    TransactionType Type,
    Guid CashAccountId,
    Guid CategoryId,
    decimal Amount,
    string? Reference,
    string? Notes) : IRequest;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(80);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public UpdateTransactionCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var txn = await _db.FinanceTransactions.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        await TransactionGuards.EnsureValidAsync(_db, request.Type, request.CategoryId, request.CashAccountId, cancellationToken);

        txn.Update(
            request.OccurredOn, request.Type,
            request.CashAccountId, request.CategoryId, request.Amount,
            request.Reference, request.Notes);

        // Re-mirror in the GL: void the prior journal(s) and post a fresh balanced entry.
        var revision = await Posting.GeneralLedgerPoster.VoidMirrorsAsync(_db, txn.Number, cancellationToken);
        await Posting.GeneralLedgerPoster.PostMirrorAsync(_db, txn, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken, $"-R{revision}");

        await _db.SaveChangesAsync(cancellationToken);
    }
}
