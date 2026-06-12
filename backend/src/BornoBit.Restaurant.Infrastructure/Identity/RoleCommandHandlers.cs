using BornoBit.Restaurant.Application.RoleManagement;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Infrastructure.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Infrastructure.Identity;

public class GetRoleListQueryHandler : IRequestHandler<GetRoleListQuery, IReadOnlyList<RoleListItemDto>>
{
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ApplicationDbContext _db;

    public GetRoleListQueryHandler(RoleManager<ApplicationRole> roles, ApplicationDbContext db)
    {
        _roles = roles;
        _db = db;
    }

    public async Task<IReadOnlyList<RoleListItemDto>> Handle(GetRoleListQuery request, CancellationToken cancellationToken)
    {
        var userCounts = await _db.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var roles = await _roles.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles
            .Select(r => new RoleListItemDto(
                r.Id,
                r.Name ?? string.Empty,
                r.Description,
                userCounts.GetValueOrDefault(r.Id),
                Roles.All.Contains(r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly RoleManager<ApplicationRole> _roles;

    public CreateRoleCommandHandler(RoleManager<ApplicationRole> roles) => _roles = roles;

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (Roles.All.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException($"'{name}' is a reserved system role name.");
        if (await _roles.RoleExistsAsync(name))
            throw new ConflictException($"A role named '{name}' already exists.");

        var role = new ApplicationRole(name)
        {
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        var result = await _roles.CreateAsync(role);
        if (!result.Succeeded)
            throw new ConflictException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return role.Id;
    }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand>
{
    private readonly RoleManager<ApplicationRole> _roles;

    public UpdateRoleCommandHandler(RoleManager<ApplicationRole> roles) => _roles = roles;

    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roles.FindByIdAsync(request.Id.ToString())
            ?? throw new NotFoundException($"Role {request.Id} not found.");

        var isSystem = Roles.All.Contains(role.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var newName = request.Name.Trim();

        if (!string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase))
        {
            if (isSystem)
                throw new ForbiddenException("System roles cannot be renamed.");
            if (Roles.All.Contains(newName, StringComparer.OrdinalIgnoreCase))
                throw new ConflictException($"'{newName}' is a reserved system role name.");

            var clash = await _roles.FindByNameAsync(newName);
            if (clash is not null && clash.Id != role.Id)
                throw new ConflictException($"A role named '{newName}' already exists.");

            role.Name = newName;
        }

        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        var result = await _roles.UpdateAsync(role);
        if (!result.Succeeded)
            throw new ConflictException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ApplicationDbContext _db;

    public DeleteRoleCommandHandler(RoleManager<ApplicationRole> roles, ApplicationDbContext db)
    {
        _roles = roles;
        _db = db;
    }

    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roles.FindByIdAsync(request.Id.ToString())
            ?? throw new NotFoundException($"Role {request.Id} not found.");

        if (Roles.All.Contains(role.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            throw new ForbiddenException("System roles cannot be deleted.");

        var assignedUsers = await _db.UserRoles.CountAsync(ur => ur.RoleId == request.Id, cancellationToken);
        if (assignedUsers > 0)
            throw new ConflictException(
                $"Role '{role.Name}' is assigned to {assignedUsers} user(s). Remove the users from the role first.");

        // AppMenuRolePermissions rows for this role are removed by the DB cascade.
        var result = await _roles.DeleteAsync(role);
        if (!result.Succeeded)
            throw new ConflictException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
