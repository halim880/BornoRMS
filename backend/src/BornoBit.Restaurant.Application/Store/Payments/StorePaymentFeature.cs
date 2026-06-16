using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Store;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Store.Payments;

// ---- Supplier payables (derived: billed − paid) ----

public record StoreSupplierPayableDto(
    Guid SupplierId,
    string Code,
    string Name,
    string? Phone,
    int PaymentTermsDays,
    decimal Billed,
    decimal Paid,
    decimal Outstanding);

public record GetStoreSupplierPayablesQuery(bool OutstandingOnly = false) : IRequest<IReadOnlyList<StoreSupplierPayableDto>>;

public class GetStoreSupplierPayablesQueryHandler : IRequestHandler<GetStoreSupplierPayablesQuery, IReadOnlyList<StoreSupplierPayableDto>>
{
    private readonly IAppDbContext _db;
    public GetStoreSupplierPayablesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoreSupplierPayableDto>> Handle(GetStoreSupplierPayablesQuery request, CancellationToken cancellationToken)
    {
        // Billed = Σ line totals of posted goods receipts (voided/draft excluded).
        var billed = await (
            from g in _db.StoreGoodsReceipts
            where g.Status == StoreGoodsReceiptStatus.Posted
            join l in _db.StoreGoodsReceiptLines on g.Id equals l.StoreGoodsReceiptId
            group l by g.StoreSupplierId into grp
            select new { SupplierId = grp.Key, Total = grp.Sum(x => x.Qty * x.UnitCost) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Total, cancellationToken);

        var paid = await _db.StorePayments
            .GroupBy(p => p.StoreSupplierId)
            .Select(g => new { SupplierId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Total, cancellationToken);

        var suppliers = await _db.StoreSuppliers
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Code, s.Name, s.Phone, s.PaymentTermsDays })
            .ToListAsync(cancellationToken);

        var rows = suppliers.Select(s =>
        {
            var b = billed.GetValueOrDefault(s.Id);
            var p = paid.GetValueOrDefault(s.Id);
            return new StoreSupplierPayableDto(s.Id, s.Code, s.Name, s.Phone, s.PaymentTermsDays, b, p, b - p);
        });

        if (request.OutstandingOnly)
            rows = rows.Where(r => r.Outstanding != 0);

        return rows.OrderByDescending(r => r.Outstanding).ToList();
    }
}

// ---- Payment history ----

public record StorePaymentDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    decimal Amount,
    DateTime PaidAtUtc,
    StorePaymentMethod Method,
    string? Reference,
    string? Notes);

public record GetStorePaymentsQuery(Guid? SupplierId = null, int Take = 100) : IRequest<IReadOnlyList<StorePaymentDto>>;

public class GetStorePaymentsQueryHandler : IRequestHandler<GetStorePaymentsQuery, IReadOnlyList<StorePaymentDto>>
{
    private readonly IAppDbContext _db;
    public GetStorePaymentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StorePaymentDto>> Handle(GetStorePaymentsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from p in _db.StorePayments
            join s in _db.StoreSuppliers on p.StoreSupplierId equals s.Id
            select new { p, s.Name };

        if (request.SupplierId is { } sid)
            query = query.Where(x => x.p.StoreSupplierId == sid);

        return await query
            .OrderByDescending(x => x.p.PaidAtUtc)
            .Take(Math.Clamp(request.Take, 1, 500))
            .Select(x => new StorePaymentDto(
                x.p.Id, x.p.StoreSupplierId, x.Name, x.p.Amount, x.p.PaidAtUtc,
                x.p.Method, x.p.Reference, x.p.Notes))
            .ToListAsync(cancellationToken);
    }
}

// ---- Record payment ----

public record RecordStorePaymentCommand(
    Guid SupplierId,
    decimal Amount,
    DateTime? PaidAtUtc,
    StorePaymentMethod Method,
    string? Reference,
    string? Notes) : IRequest<Guid>;

public class RecordStorePaymentCommandValidator : AbstractValidator<RecordStorePaymentCommand>
{
    public RecordStorePaymentCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class RecordStorePaymentCommandHandler : IRequestHandler<RecordStorePaymentCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public RecordStorePaymentCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(RecordStorePaymentCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.StoreSuppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken))
            throw new NotFoundException($"Store supplier {request.SupplierId} not found.");

        var paidAt = request.PaidAtUtc ?? _timeProvider.GetUtcNow().UtcDateTime;
        var payment = StorePayment.Create(request.SupplierId, request.Amount, paidAt, request.Method, request.Reference, request.Notes);

        _db.StorePayments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }
}
