using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Drawers;

/// <summary>The current cashier's open drawer, or null if none is open.</summary>
public record GetCurrentDrawerQuery : IRequest<DrawerDto?>;

public class GetCurrentDrawerQueryHandler : IRequestHandler<GetCurrentDrawerQuery, DrawerDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCurrentDrawerQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DrawerDto?> Handle(GetCurrentDrawerQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId) return null;

        var row = await (from d in _db.CashDrawerSessions
                         where d.CashierUserId == userId && d.Status == DrawerStatus.Open
                         join a in _db.CashAccounts on d.CashAccountId equals a.Id into acc
                         from a in acc.DefaultIfEmpty()
                         select new { d, AccountName = a != null ? a.Name : null })
                        .FirstOrDefaultAsync(cancellationToken);

        return row?.d.ToDto(row.AccountName);
    }
}

/// <summary>A drawer with its takings broken down by payment method (for the close screen).</summary>
public record GetDrawerSummaryQuery(Guid DrawerId) : IRequest<DrawerSummaryDto>;

public class GetDrawerSummaryQueryHandler : IRequestHandler<GetDrawerSummaryQuery, DrawerSummaryDto>
{
    private readonly IAppDbContext _db;
    public GetDrawerSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DrawerSummaryDto> Handle(GetDrawerSummaryQuery request, CancellationToken cancellationToken)
    {
        var row = await (from d in _db.CashDrawerSessions
                         where d.Id == request.DrawerId
                         join a in _db.CashAccounts on d.CashAccountId equals a.Id into acc
                         from a in acc.DefaultIfEmpty()
                         select new { d, AccountName = a != null ? a.Name : null })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException("Drawer not found.");

        var byMethod = await _db.Payments
            .Where(p => p.CashDrawerSessionId == request.DrawerId && p.Status == PaymentEntryStatus.Captured)
            .GroupBy(p => p.Method)
            .Select(g => new DrawerMethodLineDto(
                g.Key,
                g.Count(),
                g.Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount)))
            .ToListAsync(cancellationToken);

        return new DrawerSummaryDto(row.d.ToDto(row.AccountName), byMethod);
    }
}
