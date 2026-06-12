using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Accounting.CashAccounts;

public record SetCashAccountActiveCommand(Guid Id, bool IsActive) : IRequest;

public class SetCashAccountActiveCommandHandler : IRequestHandler<SetCashAccountActiveCommand>
{
    private readonly IAppDbContext _db;

    public SetCashAccountActiveCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SetCashAccountActiveCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.CashAccounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Cash account not found.");

        account.SetActive(request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
