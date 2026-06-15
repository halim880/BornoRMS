using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.Journals;

public record VoidJournalEntryCommand(Guid Id) : IRequest<Unit>;

public class VoidJournalEntryCommandHandler : IRequestHandler<VoidJournalEntryCommand, Unit>
{
    private readonly IAppDbContext _db;

    public VoidJournalEntryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(VoidJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Journal entry not found.");

        try
        {
            entry.Void();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
