using MediatR;

namespace BornoBit.Restaurant.Application.MenuPermissions;

public record RoleDto(Guid Id, string Name);

public record GetRolesQuery(bool IncludeSuperAdmin = false) : IRequest<IReadOnlyList<RoleDto>>;
