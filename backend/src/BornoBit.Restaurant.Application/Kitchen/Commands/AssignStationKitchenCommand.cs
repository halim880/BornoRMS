using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Kitchen.Commands;

/// <summary>Routes a kitchen station to a kitchen (null clears the assignment → falls back to the default kitchen).</summary>
public record AssignStationKitchenCommand(Guid StationId, Guid? KitchenId) : IRequest<Unit>;

public class AssignStationKitchenCommandValidator : AbstractValidator<AssignStationKitchenCommand>
{
    public AssignStationKitchenCommandValidator() => RuleFor(x => x.StationId).NotEmpty();
}

public class AssignStationKitchenCommandHandler : IRequestHandler<AssignStationKitchenCommand, Unit>
{
    private readonly IAppDbContext _db;
    public AssignStationKitchenCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(AssignStationKitchenCommand request, CancellationToken cancellationToken)
    {
        var station = await _db.KitchenStations.FirstOrDefaultAsync(s => s.Id == request.StationId, cancellationToken)
            ?? throw new NotFoundException("Kitchen station not found.");

        if (request.KitchenId is { } kid)
        {
            var kitchenOk = await _db.Kitchens.AnyAsync(k => k.Id == kid && k.IsActive, cancellationToken);
            if (!kitchenOk) throw new NotFoundException("Kitchen not found.");
        }

        station.AssignKitchen(request.KitchenId);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
