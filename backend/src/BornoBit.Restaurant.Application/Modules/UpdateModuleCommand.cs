using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Modules;

public record UpdateModuleCommand(
    Guid Id,
    string Title,
    string? Icon,
    int DisplayOrder,
    string? RequiredRole
) : IRequest<Unit>;

public class UpdateModuleCommandValidator : AbstractValidator<UpdateModuleCommand>
{
    public UpdateModuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Icon).MaximumLength(60);
        RuleFor(x => x.RequiredRole).MaximumLength(60);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateModuleCommandHandler : IRequestHandler<UpdateModuleCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;

    public UpdateModuleCommandHandler(IAppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<Unit> Handle(UpdateModuleCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsInRole(Roles.SuperAdmin))
            throw new ForbiddenException("Only SuperAdmin can update modules.");

        var entity = await _db.AppMenus
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.ParentId == null, cancellationToken)
            ?? throw new NotFoundException($"Module {request.Id} not found.");

        var title = request.Title.Trim();
        if (!string.Equals(entity.Title, title, StringComparison.OrdinalIgnoreCase))
        {
            var clash = await _db.AppMenus
                .AnyAsync(m => m.ParentId == null && m.Id != request.Id && m.Title == title, cancellationToken);
            if (clash) throw new ValidationException($"A module named '{title}' already exists.");
            entity.Title = title;
        }

        entity.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.RequiredRole = string.IsNullOrWhiteSpace(request.RequiredRole) ? null : request.RequiredRole.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
