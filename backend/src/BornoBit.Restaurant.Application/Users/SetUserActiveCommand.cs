using MediatR;

namespace BornoBit.Restaurant.Application.Users;

public record SetUserActiveCommand(Guid Id, bool IsActive) : IRequest;
