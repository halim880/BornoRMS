using BornoBit.Restaurant.Application.Accounting.Audit;
using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Drawers;

/// <summary>
/// Opens a cash-drawer shift for the current cashier with a starting float. One open shift per cashier.
/// Defaults to the first active Cash-kind account when none is given.
/// </summary>
public record OpenDrawerCommand(decimal OpeningBalance, Guid? CashAccountId = null, string? Notes = null)
    : IRequest<DrawerDto>;

public class OpenDrawerCommandValidator : AbstractValidator<OpenDrawerCommand>
{
    public OpenDrawerCommandValidator()
    {
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}

public class OpenDrawerCommandHandler : IRequestHandler<OpenDrawerCommand, DrawerDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDrawerNumberGenerator _numbers;

    public OpenDrawerCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDrawerNumberGenerator numbers)
    {
        _db = db;
        _currentUser = currentUser;
        _numbers = numbers;
    }

    public async Task<DrawerDto> Handle(OpenDrawerCommand request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager, Roles.Cashier);

        if (_currentUser.UserId is not { } userId)
            throw new ForbiddenException("You must be signed in to open a drawer.");

        var alreadyOpen = await _db.CashDrawerSessions
            .AnyAsync(d => d.CashierUserId == userId && d.Status == DrawerStatus.Open, cancellationToken);
        if (alreadyOpen) throw new ConflictException("You already have an open drawer. Close it before opening another.");

        var account = request.CashAccountId is { } accId
            ? await _db.CashAccounts.FirstOrDefaultAsync(a => a.Id == accId, cancellationToken)
                ?? throw new NotFoundException("Cash account not found.")
            : await _db.CashAccounts.FirstOrDefaultAsync(a => a.Kind == CashAccountKind.Cash && a.IsActive, cancellationToken)
                ?? throw new ConflictException("No active cash account is configured.");

        var number = await _numbers.NextAsync(DateTime.UtcNow, cancellationToken);
        var drawer = CashDrawerSession.Open(number, userId, _currentUser.UserName ?? "cashier", account.Id, request.OpeningBalance, request.Notes);
        _db.CashDrawerSessions.Add(drawer);

        FinancialAudit.Write(_db, FinancialAuditAction.DrawerOpened, _currentUser, nameof(CashDrawerSession), drawer.Id,
            amount: request.OpeningBalance, after: FinancialAudit.Snapshot(drawer), notes: $"Opened {drawer.DrawerNumber}");

        await _db.SaveChangesAsync(cancellationToken);
        return drawer.ToDto(account.Name);
    }
}
