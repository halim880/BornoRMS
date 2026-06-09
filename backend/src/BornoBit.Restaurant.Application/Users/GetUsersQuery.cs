using MediatR;

namespace BornoBit.Restaurant.Application.Users;

public record GetUsersQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<UserDto>>;
