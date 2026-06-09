using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Shared.Common;
using BornoBit.Restaurant.Shared.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Modules;

public record SetModuleActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class SetModuleActiveCommandValidator : AbstractValidator<SetModuleActiveCommand>
{
    public SetModuleActiveCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class SetModuleActiveCommandHandler : IRequestHandler<SetModuleActiveCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;

    public SetModuleActiveCommandHandler(IAppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<Unit> Handle(SetModuleActiveCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsInRole(Roles.SuperAdmin))
            throw new ForbiddenException("Only SuperAdmin can change module active state.");

        var entity = await _db.AppMenus
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.ParentId == null, cancellationToken)
            ?? throw new NotFoundException($"Module {request.Id} not found.");

        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
