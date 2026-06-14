using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Security;
using BornoBit.Restaurant.Domain.Accounting;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Audit;

/// <summary>Paged financial audit trail viewer. Manager/Admin only.</summary>
public record GetFinancialAuditQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    FinancialAuditAction? Action = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<FinancialAuditRowDto>>;

public record FinancialAuditRowDto(
    Guid Id,
    DateTime TimestampUtc,
    string UserName,
    FinancialAuditAction Action,
    string EntityType,
    Guid EntityId,
    string? OrderNumber,
    decimal? Amount,
    string? BeforeJson,
    string? AfterJson,
    string? Notes);

public class GetFinancialAuditQueryHandler : IRequestHandler<GetFinancialAuditQuery, PagedResult<FinancialAuditRowDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetFinancialAuditQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<FinancialAuditRowDto>> Handle(GetFinancialAuditQuery request, CancellationToken cancellationToken)
    {
        PermissionGuard.Require(_currentUser, Roles.Admin, Roles.Manager);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.FinancialAuditLogs.AsQueryable();

        if (request.FromUtc is { } from) query = query.Where(a => a.TimestampUtc >= from);
        if (request.ToUtc is { } to) query = query.Where(a => a.TimestampUtc < to);
        if (request.Action is { } action) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(a => (a.OrderNumber != null && a.OrderNumber.Contains(request.Search))
                || a.UserName.Contains(request.Search));

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new FinancialAuditRowDto(
                a.Id, a.TimestampUtc, a.UserName, a.Action, a.EntityType, a.EntityId,
                a.OrderNumber, a.Amount, a.BeforeJson, a.AfterJson, a.Notes))
            .ToListAsync(cancellationToken);

        return new PagedResult<FinancialAuditRowDto>(items, page, pageSize, total);
    }
}
