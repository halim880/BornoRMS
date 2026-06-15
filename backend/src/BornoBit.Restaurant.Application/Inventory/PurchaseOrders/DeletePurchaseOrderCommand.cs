using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

/// <summary>Delete a Draft purchase order. Approved/received POs cannot be removed (cancel instead).</summary>
public record DeletePurchaseOrderCommand(Guid Id) : IRequest<Unit>;

public class DeletePurchaseOrderCommandHandler : IRequestHandler<DeletePurchaseOrderCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DeletePurchaseOrderCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(DeletePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Purchase order {request.Id} not found.");

        if (po.Status != PurchaseOrderStatus.Draft)
            throw new ValidationException("Only draft purchase orders can be deleted. Cancel an approved order instead.");

        _db.PurchaseOrders.Remove(po);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
