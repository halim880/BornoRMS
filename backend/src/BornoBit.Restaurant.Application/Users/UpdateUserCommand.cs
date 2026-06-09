using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Application.Users;

public record UpdateUserCommand(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles
) : IRequest;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserName)
            .NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("Username may contain letters, digits, dot, underscore and hyphen.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Roles).NotNull();
    }
}
