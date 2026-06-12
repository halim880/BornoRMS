using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Application.RoleManagement;

public record CreateRoleCommand(string Name, string? Description) : IRequest<Guid>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(256)
            .Matches("^[A-Za-z0-9 ._-]+$")
            .WithMessage("Role name may contain letters, digits, spaces, dot, underscore and hyphen.");
        RuleFor(x => x.Description).MaximumLength(512);
    }
}
