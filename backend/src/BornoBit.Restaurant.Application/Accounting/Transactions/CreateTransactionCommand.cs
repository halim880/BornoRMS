using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Transactions;

public record CreateTransactionCommand(
    DateTime OccurredOn,
    TransactionType Type,
    Guid CashAccountId,
    Guid CategoryId,
    decimal Amount,
    string? Reference,
    string? Notes) : IRequest<Guid>;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(80);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ITransactionNumberGenerator _numbers;
    private readonly TimeProvider _timeProvider;

    public CreateTransactionCommandHandler(IAppDbContext db, ITransactionNumberGenerator numbers, TimeProvider timeProvider)
    {
        _db = db;
        _numbers = numbers;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        await TransactionGuards.EnsureValidAsync(_db, request.Type, request.CategoryId, request.CashAccountId, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var number = await _numbers.NextAsync(nowUtc, cancellationToken);

        var txn = FinanceTransaction.Create(
            number, request.OccurredOn, request.Type,
            request.CashAccountId, request.CategoryId, request.Amount,
            request.Reference, request.Notes);

        _db.FinanceTransactions.Add(txn);
        await Posting.GeneralLedgerPoster.PostMirrorAsync(_db, txn, nowUtc, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return txn.Id;
    }
}
