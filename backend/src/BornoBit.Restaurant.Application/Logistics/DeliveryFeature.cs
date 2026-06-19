using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Logistics;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Logistics;

/// <summary>
/// Dispatch board + lifecycle for delivery orders. The board lists every active delivery order (joined to
/// its <see cref="Delivery"/> tracking row, synthesising Pending when none exists yet) with its COD-expected
/// balance. Commands assign a rider, dispatch, deliver, fail, or cancel — keeping the order's own status in
/// step (dispatch fast-forwards the order to Ready; delivered moves it to Served so a later COD settle can
/// auto-complete it). COD itself is collected by the cashier on the rider's return via the normal payment flow.
/// </summary>

// ---------- board ----------

public record GetDeliveryBoardQuery(DateOnly? Date = null, bool UnpaidOnly = false, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<DeliveryBoardRow>>;

public record DeliveryBoardRow(
    Guid OrderId,
    string OrderNumber,
    DeliveryStatus DeliveryStatus,
    OrderStatus OrderStatus,
    Guid? RiderId,
    string? RiderName,
    string Address,
    string? ContactPhone,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal CodExpected,
    bool IsPaid,
    DateTime OrderedAtUtc,
    DateTime? OutForDeliveryAtUtc);

public class GetDeliveryBoardQueryHandler : IRequestHandler<GetDeliveryBoardQuery, PagedResult<DeliveryBoardRow>>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;
    public GetDeliveryBoardQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<DeliveryBoardRow>> Handle(GetDeliveryBoardQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.Orders.Where(o => o.OrderType == OrderType.Delivery && o.Status != OrderStatus.Cancelled);

        if (request.Date is { } date)
        {
            var (from, to) = _clock.DayWindowUtc(date);
            query = query.Where(o => o.OrderedAtUtc >= from && o.OrderedAtUtc < to);
        }
        if (request.UnpaidOnly) query = query.Where(o => !o.IsPaid);

        var total = await query.LongCountAsync(cancellationToken);

        var raw = await query
            .OrderByDescending(o => o.OrderedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.IsPaid,
                o.OrderedAtUtc,
                o.Notes,
                Subtotal = o.Lines.Sum(l => l.UnitPriceSnapshot * l.Quantity),
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.TipAmount,
                o.DeliveryChargeAmount,
                o.RoundingAdjustment,
                Paid = _db.Payments
                    .Where(p => p.OrderId == o.Id && p.Status == PaymentEntryStatus.Captured)
                    .Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount),
                Delivery = _db.Deliveries.Where(d => d.OrderId == o.Id).Select(d => new
                {
                    d.Status,
                    d.RiderId,
                    d.AddressLine,
                    d.ContactPhone,
                    d.OutForDeliveryAtUtc,
                    RiderName = d.RiderId == null ? null : _db.Riders.Where(r => r.Id == d.RiderId).Select(r => r.Name).FirstOrDefault()
                }).FirstOrDefault(),
                CustomerAddress = _db.Customers.Where(c => c.Id == o.CustomerId).Select(c => c.Address).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var items = raw.Select(r =>
        {
            var grand = Math.Max(0m, r.Subtotal - r.DiscountAmount + r.TaxAmount + r.ServiceChargeAmount + r.TipAmount + r.DeliveryChargeAmount + r.RoundingAdjustment);
            var balance = Math.Max(0m, grand - r.Paid);
            var status = r.Delivery?.Status ?? DeliveryStatus.Pending;
            var address = r.Delivery?.AddressLine
                ?? r.CustomerAddress
                ?? (r.Notes != null && r.Notes.StartsWith("Address: ") ? r.Notes["Address: ".Length..] : null)
                ?? "—";
            return new DeliveryBoardRow(
                r.Id, r.OrderNumber, status, r.Status,
                r.Delivery?.RiderId, r.Delivery?.RiderName,
                address, r.Delivery?.ContactPhone,
                grand, r.Paid, balance, r.IsPaid, r.OrderedAtUtc, r.Delivery?.OutForDeliveryAtUtc);
        }).ToList();

        return new PagedResult<DeliveryBoardRow>(items, page, pageSize, total);
    }
}

// ---------- COD reconciliation ----------

public record GetRiderCodReconciliationQuery(DateOnly? Date = null) : IRequest<IReadOnlyList<RiderCodRow>>;

public record RiderCodRow(
    Guid RiderId,
    string RiderName,
    int OutstandingCount,
    decimal OutstandingCod,
    int CollectedCount,
    decimal CollectedToday);

public class GetRiderCodReconciliationQueryHandler : IRequestHandler<GetRiderCodReconciliationQuery, IReadOnlyList<RiderCodRow>>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;
    public GetRiderCodReconciliationQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<RiderCodRow>> Handle(GetRiderCodReconciliationQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date ?? _clock.Today;
        var (start, end) = _clock.DayWindowUtc(date);

        // Delivered orders handed out by a rider in the window. Outstanding = still-unpaid balance (COD the rider
        // owes the till); collected = what the cashier has already settled.
        var raw = await (
            from d in _db.Deliveries
            where d.Status == DeliveryStatus.Delivered && d.RiderId != null
                  && d.DeliveredAtUtc != null && d.DeliveredAtUtc >= start && d.DeliveredAtUtc < end
            join o in _db.Orders on d.OrderId equals o.Id
            join r in _db.Riders on d.RiderId equals r.Id
            select new
            {
                RiderId = r.Id,
                RiderName = r.Name,
                o.IsPaid,
                Subtotal = o.Lines.Sum(l => l.UnitPriceSnapshot * l.Quantity),
                o.DiscountAmount,
                o.TaxAmount,
                o.ServiceChargeAmount,
                o.TipAmount,
                o.DeliveryChargeAmount,
                o.RoundingAdjustment,
                Paid = _db.Payments
                    .Where(p => p.OrderId == o.Id && p.Status == PaymentEntryStatus.Captured)
                    .Sum(p => p.Kind == PaymentKind.Charge ? p.Amount : -p.Amount),
            })
            .ToListAsync(cancellationToken);

        var lines = raw.Select(x => new
        {
            x.RiderId,
            x.RiderName,
            x.IsPaid,
            x.Paid,
            Grand = Math.Max(0m, x.Subtotal - x.DiscountAmount + x.TaxAmount + x.ServiceChargeAmount + x.TipAmount + x.DeliveryChargeAmount + x.RoundingAdjustment)
        });

        return lines
            .GroupBy(x => new { x.RiderId, x.RiderName })
            .Select(g => new RiderCodRow(
                g.Key.RiderId,
                g.Key.RiderName,
                g.Count(x => !x.IsPaid),
                g.Where(x => !x.IsPaid).Sum(x => Math.Max(0m, x.Grand - x.Paid)),
                g.Count(x => x.IsPaid),
                g.Where(x => x.IsPaid).Sum(x => x.Paid)))
            .OrderByDescending(r => r.OutstandingCod)
            .ToList();
    }
}

// ---------- lifecycle commands ----------

public record AssignRiderCommand(Guid OrderId, Guid RiderId) : IRequest<Unit>;
public record MarkOutForDeliveryCommand(Guid OrderId) : IRequest<Unit>;
public record MarkDeliveredCommand(Guid OrderId) : IRequest<Unit>;
public record MarkDeliveryFailedCommand(Guid OrderId, string? Reason) : IRequest<Unit>;
public record CancelDeliveryCommand(Guid OrderId, string? Reason) : IRequest<Unit>;

public class DeliveryCommandHandler :
    IRequestHandler<AssignRiderCommand, Unit>,
    IRequestHandler<MarkOutForDeliveryCommand, Unit>,
    IRequestHandler<MarkDeliveredCommand, Unit>,
    IRequestHandler<MarkDeliveryFailedCommand, Unit>,
    IRequestHandler<CancelDeliveryCommand, Unit>
{
    private readonly IAppDbContext _db;
    public DeliveryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(AssignRiderCommand request, CancellationToken cancellationToken)
    {
        var rider = await _db.Riders.FirstOrDefaultAsync(r => r.Id == request.RiderId && r.IsActive, cancellationToken)
            ?? throw new NotFoundException("Rider not found or inactive.");
        var delivery = await GetOrCreateAsync(request.OrderId, cancellationToken);
        delivery.Assign(rider.Id);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(MarkOutForDeliveryCommand request, CancellationToken cancellationToken)
    {
        var delivery = await GetDeliveryAsync(request.OrderId, cancellationToken);
        var order = await GetOrderAsync(request.OrderId, cancellationToken);

        // Fast-forward the food status so the order is reliably Ready before it leaves — otherwise a later
        // delivered→Served transition (and the COD auto-complete) would dead-end.
        FastForwardToReady(order);
        delivery.MarkOutForDelivery();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(MarkDeliveredCommand request, CancellationToken cancellationToken)
    {
        var delivery = await GetDeliveryAsync(request.OrderId, cancellationToken);
        var order = await GetOrderAsync(request.OrderId, cancellationToken);

        delivery.MarkDelivered();
        if (order.Status == OrderStatus.Ready) order.MarkServed();
        // Prepaid (online/card) orders complete now; COD orders complete when the cashier settles on return.
        if (order.Status == OrderStatus.Served && order.IsPaid) order.Complete();
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(MarkDeliveryFailedCommand request, CancellationToken cancellationToken)
    {
        var delivery = await GetDeliveryAsync(request.OrderId, cancellationToken);
        delivery.MarkFailed(request.Reason);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    public async Task<Unit> Handle(CancelDeliveryCommand request, CancellationToken cancellationToken)
    {
        var delivery = await GetDeliveryAsync(request.OrderId, cancellationToken);
        var order = await GetOrderAsync(request.OrderId, cancellationToken);

        delivery.Cancel();
        // Mirror to the order when it can still be cancelled (not paid, not yet served).
        if (!order.IsPaid && order.Status is not (OrderStatus.Served or OrderStatus.Completed or OrderStatus.Cancelled))
            order.Cancel(request.Reason);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private static void FastForwardToReady(Order order)
    {
        if (order.Status is OrderStatus.Ready or OrderStatus.Served or OrderStatus.Completed or OrderStatus.Cancelled) return;
        if (order.Status == OrderStatus.Placed) order.Confirm();
        if (order.Status == OrderStatus.Confirmed) order.StartPreparing();
        if (order.Status == OrderStatus.Preparing) order.MarkReady();
    }

    private async Task<Order> GetOrderAsync(Guid orderId, CancellationToken ct) =>
        await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException("Order not found.");

    private async Task<Delivery> GetDeliveryAsync(Guid orderId, CancellationToken ct) =>
        await _db.Deliveries.FirstOrDefaultAsync(d => d.OrderId == orderId, ct)
            ?? throw new NotFoundException("No delivery record for this order.");

    // Defensive lazy-create for a delivery order placed before the create-hook existed (or via another path).
    private async Task<Delivery> GetOrCreateAsync(Guid orderId, CancellationToken ct)
    {
        var existing = await _db.Deliveries.FirstOrDefaultAsync(d => d.OrderId == orderId, ct);
        if (existing is not null) return existing;

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException("Order not found.");
        if (order.OrderType != OrderType.Delivery)
            throw new ConflictException("Order is not a delivery order.");

        var address = await _db.Customers.Where(c => c.Id == order.CustomerId).Select(c => c.Address).FirstOrDefaultAsync(ct)
            ?? (order.Notes != null && order.Notes.StartsWith("Address: ") ? order.Notes["Address: ".Length..] : null)
            ?? "—";
        var delivery = Delivery.Create(order.Id, address, null);
        _db.Deliveries.Add(delivery);
        return delivery;
    }
}
