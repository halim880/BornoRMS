using FluentValidation;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Numbering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Numbering;

public record UpdateNumberingScopeCommand(
    Guid Id,
    string Name,
    string Prefix,
    NumberingCadence Cadence,
    byte Digits,
    bool ResetByOutlet
) : IRequest<Unit>;

public class UpdateNumberingScopeCommandValidator : AbstractValidator<UpdateNumberingScopeCommand>
{
    public UpdateNumberingScopeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Prefix).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Digits).InclusiveBetween((byte)1, (byte)10);
    }
}

public class UpdateNumberingScopeCommandHandler : IRequestHandler<UpdateNumberingScopeCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateNumberingScopeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateNumberingScopeCommand request, CancellationToken cancellationToken)
    {
        var scope = await _db.NumberingScopes.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Numbering scope {request.Id} not found.");

        scope.UpdateDetails(request.Name, request.Prefix, request.Cadence, request.Digits, request.ResetByOutlet);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
