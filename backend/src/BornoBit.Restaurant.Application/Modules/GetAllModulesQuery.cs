using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Modules;

/// <summary>
/// Returns all root-level AppMenu rows (active and inactive) for the Modules admin page.
/// SuperAdmin only.
/// </summary>
public record GetAllModulesQuery : IRequest<IReadOnlyList<ModuleDto>>;

public class GetAllModulesQueryHandler : IRequestHandler<GetAllModulesQuery, IReadOnlyList<ModuleDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;

    public GetAllModulesQueryHandler(IAppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<IReadOnlyList<ModuleDto>> Handle(GetAllModulesQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsInRole(Roles.SuperAdmin))
            throw new ForbiddenException("Only SuperAdmin can manage modules.");

        var roots = await _db.AppMenus
            .Where(m => m.ParentId == null)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Title)
            .Select(m => new
            {
                m.Id, m.Title, m.Icon, m.DisplayOrder, m.IsActive, m.RequiredRole, m.Url,
                ChildCount = _db.AppMenus.Count(c => c.ParentId == m.Id)
            })
            .ToListAsync(cancellationToken);

        return roots.Select(m => new ModuleDto(
            m.Id, m.Title, m.Icon, m.DisplayOrder, m.IsActive, m.RequiredRole,
            FirstAccessibleUrl: m.Url,
            AccessibleMenuCount: m.ChildCount)).ToList();
    }
}
