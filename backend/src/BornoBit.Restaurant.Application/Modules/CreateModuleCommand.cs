using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Domain.Menus;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Modules;

public record CreateModuleCommand(
    string Title,
    string? Icon,
    int? DisplayOrder,
    string? RequiredRole
) : IRequest<Guid>;

public class CreateModuleCommandValidator : AbstractValidator<CreateModuleCommand>
{
    public CreateModuleCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Icon).MaximumLength(60);
        RuleFor(x => x.RequiredRole).MaximumLength(60);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);
    }
}

public class CreateModuleCommandHandler : IRequestHandler<CreateModuleCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;

    public CreateModuleCommandHandler(IAppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<Guid> Handle(CreateModuleCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsInRole(Roles.SuperAdmin))
            throw new ForbiddenException("Only SuperAdmin can create modules.");

        var title = request.Title.Trim();
        var clash = await _db.AppMenus
            .AnyAsync(m => m.ParentId == null && m.Title == title, cancellationToken);
        if (clash) throw new ValidationException($"A module named '{title}' already exists.");

        var nextOrder = request.DisplayOrder
            ?? ((await _db.AppMenus
                .Where(m => m.ParentId == null)
                .Select(m => (int?)m.DisplayOrder)
                .MaxAsync(cancellationToken)) ?? -1) + 1;

        var entity = new AppMenu
        {
            Title = title,
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            ParentId = null,
            Url = null,
            DisplayOrder = nextOrder,
            IsActive = true,
            RequiredRole = string.IsNullOrWhiteSpace(request.RequiredRole) ? null : request.RequiredRole.Trim()
        };
        _db.AppMenus.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
