using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Menus;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.MenuPermissions;

public record SaveMenuPermissionsCommand(
    Guid RoleId,
    IReadOnlyList<Guid> PermittedMenuIds
) : IRequest;

public class SaveMenuPermissionsCommandValidator : AbstractValidator<SaveMenuPermissionsCommand>
{
    public SaveMenuPermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermittedMenuIds).NotNull();
    }
}

public class SaveMenuPermissionsCommandHandler : IRequestHandler<SaveMenuPermissionsCommand>
{
    private readonly IAppDbContext _db;

    public SaveMenuPermissionsCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SaveMenuPermissionsCommand request, CancellationToken cancellationToken)
    {
        var desired = request.PermittedMenuIds.Distinct().ToHashSet();

        var validMenuIds = (await _db.AppMenus
            .Where(m => desired.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var existing = await _db.AppMenuRolePermissions
            .Where(p => p.RoleId == request.RoleId)
            .ToListAsync(cancellationToken);

        var existingIds = existing.Select(p => p.MenuId).ToHashSet();

        foreach (var row in existing.Where(p => !validMenuIds.Contains(p.MenuId)))
            _db.AppMenuRolePermissions.Remove(row);

        foreach (var menuId in validMenuIds.Where(id => !existingIds.Contains(id)))
        {
            _db.AppMenuRolePermissions.Add(new AppMenuRolePermission
            {
                MenuId = menuId,
                RoleId = request.RoleId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
