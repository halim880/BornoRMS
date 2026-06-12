using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.CashAccounts;

public record UpdateCashAccountCommand(
    Guid Id,
    string Name,
    CashAccountKind Kind,
    decimal OpeningBalance) : IRequest;

public class UpdateCashAccountCommandValidator : AbstractValidator<UpdateCashAccountCommand>
{
    public UpdateCashAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Kind).IsInEnum();
    }
}

public class UpdateCashAccountCommandHandler : IRequestHandler<UpdateCashAccountCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCashAccountCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCashAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.CashAccounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Cash account not found.");

        var name = request.Name.Trim();
        if (await _db.CashAccounts.AnyAsync(a => a.Id != request.Id && a.Name == name, cancellationToken))
            throw new ConflictException($"A cash account named '{name}' already exists.");

        account.Update(name, request.Kind, request.OpeningBalance);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
