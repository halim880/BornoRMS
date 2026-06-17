using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Ordering.Queries;

/// <summary>Paid sales over a date range (UTC), one row per invoice (order).</summary>
public record GetSalesInvoiceReportQuery(DateTime From, DateTime To) : IRequest<SalesInvoiceReportDto>;

public record SalesInvoiceRowDto(
    Guid OrderId,
    string OrderNumber,
    DateTime PaidAtUtc,
    string? CustomerName,
    string CustomerPhone,
    OrderType OrderType,
    PaymentMethod? PaymentMethod,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    string Currency);

public record SalesInvoiceReportDto(
    IReadOnlyList<SalesInvoiceRowDto> Rows,
    int TotalInvoices,
    decimal TotalSubtotal,
    decimal TotalDiscount,
    decimal GrandTotal,
    string Currency);

public class GetSalesInvoiceReportQueryHandler : IRequestHandler<GetSalesInvoiceReportQuery, SalesInvoiceReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetSalesInvoiceReportQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SalesInvoiceReportDto> Handle(GetSalesInvoiceReportQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        // Order.Total is a C# computed property over the lines navigation, which EF cannot
        // translate, so recompute subtotal/total inline (mirrors GetSalesReportQuery/GetOrdersQuery).
        var rows = await (
            from o in _db.Orders
            join c in _db.Customers on o.CustomerId equals c.Id
            where o.IsPaid
                  && o.Status != OrderStatus.Cancelled
                  && o.PaidAtUtc != null
                  && o.PaidAtUtc >= start
                  && o.PaidAtUtc < end
            orderby o.PaidAtUtc
            let subtotal = o.Lines.Sum(l => (decimal?)l.UnitPriceSnapshot * l.Quantity) ?? 0m
            select new SalesInvoiceRowDto(
                o.Id,
                o.OrderNumber,
                o.PaidAtUtc!.Value,
                c.FullName,
                c.Phone,
                o.OrderType,
                o.PaymentMethod,
                subtotal,
                o.DiscountAmount,
                subtotal - o.DiscountAmount + o.RoundingAdjustment < 0m
                    ? 0m
                    : subtotal - o.DiscountAmount + o.RoundingAdjustment,
                o.Currency))
            .ToListAsync(cancellationToken);

        var currency = rows.Select(r => r.Currency).FirstOrDefault() ?? "Tk";

        return new SalesInvoiceReportDto(
            rows,
            rows.Count,
            rows.Sum(r => r.Subtotal),
            rows.Sum(r => r.Discount),
            rows.Sum(r => r.Total),
            currency);
    }
}
