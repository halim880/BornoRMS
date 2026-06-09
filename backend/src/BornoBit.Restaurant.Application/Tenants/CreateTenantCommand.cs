using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Tenants;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Tenants;

public record CreateTenantCommand(
    string Name,
    string Subdomain,
    string ContactEmail,
    DateTime? LicenseExpiresOnUtc
) : IRequest<Guid>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .MaximumLength(63)
            .Matches("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")
            .WithMessage("Subdomain must be lowercase alphanumeric or hyphens (no leading/trailing hyphen).");
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateTenantCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var subdomain = request.Subdomain.Trim().ToLowerInvariant();

        if (await _db.Tenants.AnyAsync(t => t.Subdomain == subdomain, cancellationToken))
            throw new ConflictException($"A tenant with subdomain '{subdomain}' already exists.");

        var tenant = Tenant.Create(request.Name, subdomain, request.ContactEmail, request.LicenseExpiresOnUtc);
        _db.Tenants.Add(tenant);

        await _db.SaveChangesAsync(cancellationToken);
        return tenant.Id;
    }
}
