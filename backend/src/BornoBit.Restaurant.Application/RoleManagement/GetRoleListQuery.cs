using MediatR;

namespace BornoBit.Restaurant.Application.RoleManagement;

public record RoleListItemDto(
    Guid Id,
    string Name,
    string? Description,
    int UserCount,
    bool IsSystem
);

// Handler lives in Infrastructure (RoleCommandHandlers.cs) — needs RoleManager, so it is Web-only.
public record GetRoleListQuery() : IRequest<IReadOnlyList<RoleListItemDto>>;
