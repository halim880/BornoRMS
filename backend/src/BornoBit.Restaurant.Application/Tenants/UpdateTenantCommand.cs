using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Tenants;

public record UpdateTenantCommand(
    Guid Id,
    string Name,
    string ContactEmail,
    DateTime? LicenseExpiresOnUtc
) : IRequest;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand>
{
    private readonly IAppDbContext _db;

    public UpdateTenantCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Tenant {request.Id} not found.");

        tenant.UpdateDetails(request.Name, request.ContactEmail, request.LicenseExpiresOnUtc);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
