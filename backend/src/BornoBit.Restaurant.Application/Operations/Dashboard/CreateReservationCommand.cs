using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>Creates a lightweight table booking (powers the dashboard "Reserved" status + quick action).</summary>
public record CreateReservationCommand(
    Guid TableId,
    string CustomerName,
    string? Phone,
    int PartySize,
    DateTime ReservedForUtc,
    string? Note = null) : IRequest<Guid>;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.TableId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.PartySize).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateReservationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var tableOk = await _db.RestaurantTables.AnyAsync(t => t.Id == request.TableId && t.IsActive, cancellationToken);
        if (!tableOk) throw new NotFoundException("Table not found.");

        var entity = TableReservation.Create(
            request.TableId, request.CustomerName, request.Phone, request.PartySize, request.ReservedForUtc, request.Note);

        _db.TableReservations.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
