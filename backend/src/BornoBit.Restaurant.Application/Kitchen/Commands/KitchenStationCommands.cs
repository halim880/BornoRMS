using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Kitchen;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Kitchen.Commands;

public record CreateKitchenStationCommand(string Name, string? Code, string? ColorHex, int DisplayOrder) : IRequest<Guid>;
public record UpdateKitchenStationCommand(Guid Id, string Name, string? Code, string? ColorHex, int DisplayOrder) : IRequest<Unit>;
public record ToggleKitchenStationActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class CreateKitchenStationCommandValidator : AbstractValidator<CreateKitchenStationCommand>
{
    public CreateKitchenStationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Code).MaximumLength(20);
        RuleFor(x => x.ColorHex).MaximumLength(9);
    }
}

public class UpdateKitchenStationCommandValidator : AbstractValidator<UpdateKitchenStationCommand>
{
    public UpdateKitchenStationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Code).MaximumLength(20);
        RuleFor(x => x.ColorHex).MaximumLength(9);
    }
}

public class CreateKitchenStationCommandHandler : IRequestHandler<CreateKitchenStationCommand, Guid>
{
    private readonly IAppDbContext _db;
    public CreateKitchenStationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateKitchenStationCommand request, CancellationToken cancellationToken)
    {
        var station = KitchenStation.Create(request.Name, request.Code, request.ColorHex, request.DisplayOrder);
        _db.KitchenStations.Add(station);
        await _db.SaveChangesAsync(cancellationToken);
        return station.Id;
    }
}

public class UpdateKitchenStationCommandHandler : IRequestHandler<UpdateKitchenStationCommand, Unit>
{
    private readonly IAppDbContext _db;
    public UpdateKitchenStationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateKitchenStationCommand request, CancellationToken cancellationToken)
    {
        var station = await _db.KitchenStations.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Kitchen station not found.");
        station.UpdateDetails(request.Name, request.Code, request.ColorHex, request.DisplayOrder);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class ToggleKitchenStationActiveCommandHandler : IRequestHandler<ToggleKitchenStationActiveCommand, Unit>
{
    private readonly IAppDbContext _db;
    public ToggleKitchenStationActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(ToggleKitchenStationActiveCommand request, CancellationToken cancellationToken)
    {
        var station = await _db.KitchenStations.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Kitchen station not found.");
        if (request.IsActive) station.Activate(); else station.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
