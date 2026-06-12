using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.CashAccounts;

public record CreateCashAccountCommand(
    string Name,
    CashAccountKind Kind,
    decimal OpeningBalance) : IRequest<Guid>;

public class CreateCashAccountCommandValidator : AbstractValidator<CreateCashAccountCommand>
{
    public CreateCashAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Kind).IsInEnum();
    }
}

public class CreateCashAccountCommandHandler : IRequestHandler<CreateCashAccountCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateCashAccountCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateCashAccountCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await _db.CashAccounts.AnyAsync(a => a.Name == name, cancellationToken))
            throw new ConflictException($"A cash account named '{name}' already exists.");

        var account = CashAccount.Create(name, request.Kind, request.OpeningBalance);
        _db.CashAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account.Id;
    }
}
