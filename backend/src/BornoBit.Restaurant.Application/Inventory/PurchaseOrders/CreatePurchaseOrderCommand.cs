using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.PurchaseOrders;

public record PurchaseOrderLineInput(Guid ItemId, decimal Qty, Guid UnitId, decimal UnitCost);

/// <summary>Create a Draft purchase order. No stock effect — approve it, then raise goods receipts against it.</summary>
public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    DateTime? OrderedAtUtc,
    DateTime? ExpectedAtUtc,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineInput> Lines
) : IRequest<Guid>;

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
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

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public CreatePurchaseOrderCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException($"Supplier {request.SupplierId} not found.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var orderedAt = request.OrderedAtUtc ?? nowUtc;

        var po = PurchaseOrder.Create(
            await NextPoNumberAsync(orderedAt, cancellationToken),
            request.SupplierId,
            orderedAt,
            request.ExpectedAtUtc,
            notes: request.Notes);

        await PurchaseOrderLineBuilder.ApplyLinesAsync(_db, po, request.Lines, cancellationToken);

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(cancellationToken);
        return po.Id;
    }

    private async Task<string> NextPoNumberAsync(DateTime orderedAtUtc, CancellationToken cancellationToken)
    {
        var dayStart = orderedAtUtc.Date;
        var dayEnd = dayStart.AddDays(1);
        var countToday = await _db.PurchaseOrders
            .CountAsync(p => p.OrderedAtUtc >= dayStart && p.OrderedAtUtc < dayEnd, cancellationToken);
        return $"PO-{dayStart:yyyyMMdd}-{countToday + 1:D4}";
    }
}
