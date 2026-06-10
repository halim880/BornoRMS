using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Accounts;

public record CreateAccountCommand(
    string Code,
    string Name,
    AccountType AccountType,
    Guid? ParentId,
    bool IsPostable,
    string? Description) : IRequest<Guid>;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.AccountType).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateAccountCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Accounts.AnyAsync(a => a.Code == code, cancellationToken))
            throw new ConflictException($"An account with code '{code}' already exists.");

        if (request.ParentId is { } parentId)
        {
            var parent = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == parentId, cancellationToken)
                ?? throw new NotFoundException("Parent account not found.");
            if (parent.IsPostable)
                throw new ConflictException("Parent must be a group (non-postable) account.");
        }

        var account = Account.Create(code, request.Name, request.AccountType, request.ParentId, request.IsPostable, request.Description);
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account.Id;
    }
}
