using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = MediatR.Unit;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

/// <summary>Replace the header and all lines of a Draft purchase order. Approved/received POs are immutable.</summary>
public record UpdatePurchaseOrderCommand(
    Guid Id,
    Guid SupplierId,
    DateTime? OrderedAtUtc,
    DateTime? ExpectedAtUtc,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineInput> Lines
) : IRequest<Unit>;

public class UpdatePurchaseOrderCommandValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UnitId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public class UpdatePurchaseOrderCommandHandler : IRequestHandler<UpdatePurchaseOrderCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public UpdatePurchaseOrderCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Purchase order {request.Id} not found.");

        if (po.Status != PurchaseOrderStatus.Draft)
            throw new ValidationException("Only draft purchase orders can be edited.");

        if (!await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        po.UpdateHeader(request.SupplierId, request.OrderedAtUtc ?? nowUtc, request.ExpectedAtUtc, request.Notes);
        po.ClearLines();
        await PurchaseOrderLineBuilder.ApplyLinesAsync(_db, po, request.Lines, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
