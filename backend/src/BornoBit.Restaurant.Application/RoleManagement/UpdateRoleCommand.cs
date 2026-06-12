using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Application.RoleManagement;

public record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(256)
            .Matches("^[A-Za-z0-9 ._-]+$")
            .WithMessage("Role name may contain letters, digits, spaces, dot, underscore and hyphen.");
        RuleFor(x => x.Description).MaximumLength(512);
    }
}
