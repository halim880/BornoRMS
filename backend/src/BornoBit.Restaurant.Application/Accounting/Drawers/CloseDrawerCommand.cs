using BornoBit.Restaurant.Application.Accounting.Audit;
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

/// <summary>Closes a drawer shift against a physical cash count, surfacing the variance.</summary>
public record CloseDrawerCommand(Guid DrawerId, decimal CountedBalance, string? Notes = null)
    : IRequest<DrawerCloseResultDto>;

public class CloseDrawerCommandValidator : AbstractValidator<CloseDrawerCommand>
{
    public CloseDrawerCommandValidator()
    {
        RuleFor(x => x.DrawerId).NotEmpty();
        RuleFor(x => x.CountedBalance).GreaterThanOrEqualTo(0);
    }
}

public class CloseDrawerCommandHandler : IRequestHandler<CloseDrawerCommand, DrawerCloseResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CloseDrawerCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DrawerCloseResultDto> Handle(CloseDrawerCommand request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager, Roles.Cashier);

        var drawer = await _db.CashDrawerSessions.FirstOrDefaultAsync(d => d.Id == request.DrawerId, cancellationToken)
            ?? throw new NotFoundException("Drawer not found.");

        // A cashier may close only their own drawer; managers/admins may close any.
        if (drawer.CashierUserId != _currentUser.UserId
            && !_currentUser.IsInRole(Roles.Admin) && !_currentUser.IsInRole(Roles.Manager) && !_currentUser.IsInRole(Roles.SuperAdmin))
            throw new ForbiddenException("You can only close your own drawer.");

        var before = FinancialAudit.Snapshot(drawer);

        try
        {
            drawer.Close(request.CountedBalance, request.Notes);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new ConflictException(ex.Message);
        }

        FinancialAudit.Write(_db, FinancialAuditAction.DrawerClosed, _currentUser, nameof(CashDrawerSession), drawer.Id,
            amount: drawer.Variance, before: before, after: FinancialAudit.Snapshot(drawer),
            notes: $"Closed {drawer.DrawerNumber}; variance {drawer.Variance:0.00}");

        await _db.SaveChangesAsync(cancellationToken);
        return new DrawerCloseResultDto(drawer.DrawerNumber, drawer.ExpectedClosingBalance, request.CountedBalance, drawer.Variance);
    }
}
