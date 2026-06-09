using FluentValidation;
using MediatR;

namespace BornoBit.Restaurant.Application.Users;

public record CreateUserCommand(
    string UserName,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    string? InitialPassword
) : IRequest<CreateUserResult>;

public record CreateUserResult(Guid UserId, string GeneratedPassword);

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("Username may contain letters, digits, dot, underscore and hyphen.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Roles).NotNull();
        RuleFor(x => x.InitialPassword)
            .MinimumLength(8).When(x => !string.IsNullOrEmpty(x.InitialPassword));
    }
}
