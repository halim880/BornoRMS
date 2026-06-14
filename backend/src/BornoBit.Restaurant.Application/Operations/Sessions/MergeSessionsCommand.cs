using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Sessions;

/// <summary>
/// Merges one or more source sessions into a survivor: their orders are re-parented onto the survivor and
/// the sources are marked Merged. Guests are summed onto the survivor.
/// </summary>
public record MergeSessionsCommand(Guid SurvivorSessionId, IReadOnlyList<Guid> SourceSessionIds) : IRequest<Unit>;

public class MergeSessionsCommandValidator : AbstractValidator<MergeSessionsCommand>
{
    public MergeSessionsCommandValidator()
    {
        RuleFor(x => x.SurvivorSessionId).NotEmpty();
        RuleFor(x => x.SourceSessionIds).NotEmpty();
    }
}

public class MergeSessionsCommandHandler : IRequestHandler<MergeSessionsCommand, Unit>
{
    private readonly IAppDbContext _db;
    public MergeSessionsCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(MergeSessionsCommand request, CancellationToken cancellationToken)
    {
        var survivor = await _db.DiningSessions.FirstOrDefaultAsync(s => s.Id == request.SurvivorSessionId, cancellationToken)
            ?? throw new NotFoundException("Survivor session not found.");
        if (survivor.Status is DiningSessionStatus.Closed or DiningSessionStatus.Merged)
            throw new ConflictException("The survivor session is no longer active.");

        var sourceIds = request.SourceSessionIds.Where(id => id != survivor.Id).Distinct().ToList();
        var sources = await _db.DiningSessions.Where(s => sourceIds.Contains(s.Id)).ToListAsync(cancellationToken);
        if (sources.Count == 0) throw new NotFoundException("No source sessions to merge.");

        var orders = await _db.Orders.Where(o => o.DiningSessionId != null && sourceIds.Contains(o.DiningSessionId.Value))
            .ToListAsync(cancellationToken);

        var addedGuests = 0;
        foreach (var source in sources)
        {
            addedGuests += source.GuestCount;
            foreach (var order in orders.Where(o => o.DiningSessionId == source.Id))
                order.AttachToSession(survivor.Id);

            try { source.MergeInto(survivor.Id); }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                throw new ConflictException(ex.Message);
            }
        }

        survivor.ChangeGuestCount(survivor.GuestCount + addedGuests);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
