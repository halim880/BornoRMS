using MediatR;

namespace BornoBit.Restaurant.Application.Users;

public record ResetPasswordCommand(Guid Id) : IRequest<string>;
