using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Menus;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.MenuPermissions;

public record SaveModulePermissionsCommand(
    Guid RoleId,
    IReadOnlyList<Guid> PermittedModuleIds
) : IRequest;

public class SaveModulePermissionsCommandValidator : AbstractValidator<SaveModulePermissionsCommand>
{
    public SaveModulePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermittedModuleIds).NotNull();
    }
}

// Root-scoped counterpart of SaveMenuPermissionsCommand: only touches permission rows whose
// MenuId is a root menu (module). Child-menu rows are owned by SaveMenuPermissionsCommand.
public class SaveModulePermissionsCommandHandler : IRequestHandler<SaveModulePermissionsCommand>
{
    private readonly IAppDbContext _db;

    public SaveModulePermissionsCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SaveModulePermissionsCommand request, CancellationToken cancellationToken)
    {
        var rootIds = (await _db.AppMenus
            .Where(m => m.ParentId == null)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var desired = request.PermittedModuleIds.Where(rootIds.Contains).ToHashSet();

        var existingRootRows = await _db.AppMenuRolePermissions
            .Where(p => p.RoleId == request.RoleId && rootIds.Contains(p.MenuId))
            .ToListAsync(cancellationToken);

        var existingIds = existingRootRows.Select(p => p.MenuId).ToHashSet();

        foreach (var row in existingRootRows.Where(p => !desired.Contains(p.MenuId)))
            _db.AppMenuRolePermissions.Remove(row);

        foreach (var moduleId in desired.Where(id => !existingIds.Contains(id)))
        {
            _db.AppMenuRolePermissions.Add(new AppMenuRolePermission
            {
                MenuId = moduleId,
                RoleId = request.RoleId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
