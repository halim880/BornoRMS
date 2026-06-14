using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Kitchen.Commands;

/// <summary>Routes a product to a kitchen station (null clears the assignment → "All").</summary>
public record AssignProductStationCommand(Guid ProductId, Guid? StationId) : IRequest<Unit>;

public class AssignProductStationCommandValidator : AbstractValidator<AssignProductStationCommand>
{
    public AssignProductStationCommandValidator() => RuleFor(x => x.ProductId).NotEmpty();
}

public class AssignProductStationCommandHandler : IRequestHandler<AssignProductStationCommand, Unit>
{
    private readonly IAppDbContext _db;
    public AssignProductStationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(AssignProductStationCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        if (request.StationId is { } sid)
        {
            var stationOk = await _db.KitchenStations.AnyAsync(s => s.Id == sid && s.IsActive, cancellationToken);
            if (!stationOk) throw new NotFoundException("Kitchen station not found.");
        }

        product.AssignStation(request.StationId);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
