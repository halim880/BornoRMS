using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Purchases;

/// <summary>Delete a Draft goods receipt. Posted receipts cannot be removed.</summary>
public record DeleteGoodsReceiptCommand(Guid Id) : IRequest<Unit>;

public class DeleteGoodsReceiptCommandHandler : IRequestHandler<DeleteGoodsReceiptCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DeleteGoodsReceiptCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        var grn = await _db.GoodsReceipts
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Goods receipt {request.Id} not found.");

        if (grn.Status != Domain.Inventory.GoodsReceiptStatus.Draft)
            throw new ValidationException("Only draft receipts can be deleted.");

        _db.GoodsReceipts.Remove(grn);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
