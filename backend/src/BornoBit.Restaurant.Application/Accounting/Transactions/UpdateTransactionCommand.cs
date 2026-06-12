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

    public UpdateTransactionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var txn = await _db.FinanceTransactions.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        await TransactionGuards.EnsureValidAsync(_db, request.Type, request.CategoryId, request.CashAccountId, cancellationToken);

        txn.Update(
            request.OccurredOn, request.Type,
            request.CashAccountId, request.CategoryId, request.Amount,
            request.Reference, request.Notes);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
