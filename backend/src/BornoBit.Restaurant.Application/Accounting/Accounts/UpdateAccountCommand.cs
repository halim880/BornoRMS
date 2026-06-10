using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Accounts;

public record UpdateAccountCommand(
    Guid Id,
    string Name,
    AccountType AccountType,
    Guid? ParentId,
    bool IsPostable,
    string? Description) : IRequest<Unit>;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.AccountType).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ParentId).NotEqual(x => x.Id).WithMessage("An account cannot be its own parent.");
    }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateAccountCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        if (request.ParentId is { } parentId)
        {
            var parent = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == parentId, cancellationToken)
                ?? throw new NotFoundException("Parent account not found.");
            if (parent.IsPostable)
                throw new ConflictException("Parent must be a group (non-postable) account.");
        }

        // Demoting a postable account to a group is unsafe once it has been used.
        if (!request.IsPostable && account.IsPostable)
        {
            var hasLines = await _db.JournalLines.AnyAsync(l => l.AccountId == account.Id, cancellationToken);
            if (hasLines) throw new ConflictException("Cannot convert an account with journal lines into a group account.");
        }

        account.Update(request.Name, request.AccountType, request.ParentId, request.IsPostable, request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
