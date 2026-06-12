using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Application.RoleManagement;

public record DeleteRoleCommand(Guid Id) : IRequest;

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
