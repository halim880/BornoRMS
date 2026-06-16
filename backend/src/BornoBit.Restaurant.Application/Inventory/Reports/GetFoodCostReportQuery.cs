using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Time;
using BornoBit.Restaurant.Domain.Inventory;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Inventory.Reports;

/// <summary>
/// Consumption-based food-cost MANAGEMENT report (the restaurant's key food-cost % metric). COGS is the
/// value of stock actually consumed (recipe explosion) over the window, valued at moving-average cost from
/// the <c>ConsumptionOut</c> ledger rows. This is deliberately NOT posted to the cash-basis financial P&amp;L
/// (that would double-count against cash purchases) — it is a standalone operational view.
/// </summary>
public record GetFoodCostReportQuery(DateTime From, DateTime To) : IRequest<FoodCostReportDto>;

public record FoodCostCategoryRow(string Category, decimal Cogs);

public record FoodCostReportDto(
    decimal NetSales,
    decimal Cogs,
    decimal FoodCostPercent,
    decimal Wastage,
    decimal InventoryValue,
    IReadOnlyList<FoodCostCategoryRow> Categories,
    string Currency);

public class GetFoodCostReportQueryHandler : IRequestHandler<GetFoodCostReportQuery, FoodCostReportDto>
{
    private readonly IAppDbContext _db;
    private readonly IBusinessClock _clock;

    public GetFoodCostReportQueryHandler(IAppDbContext db, IBusinessClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<FoodCostReportDto> Handle(GetFoodCostReportQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = _clock.RangeUtc(DateOnly.FromDateTime(request.From), DateOnly.FromDateTime(request.To));

        // COGS by stock category — value of ConsumptionOut (QtyBase is negative for stock-out, so negate).
        var byCategory = await (
            from m in _db.StockMovements
            join i in _db.InventoryItems on m.InventoryItemId equals i.Id
            join c in _db.InventoryCategories on i.InventoryCategoryId equals c.Id
            where m.MovementType == StockMovementType.ConsumptionOut
                  && m.OccurredAtUtc >= start && m.OccurredAtUtc < end
            group (-m.QtyBase) * m.UnitCost by c.Name into g
            select new FoodCostCategoryRow(g.Key, g.Sum()))
            .ToListAsync(cancellationToken);

        byCategory = byCategory.Where(r => r.Cogs != 0m).OrderByDescending(r => r.Cogs).ToList();
        var cogs = byCategory.Sum(r => r.Cogs);

        var wastage = await _db.StockMovements
            .Where(m => m.MovementType == StockMovementType.WastageOut && m.OccurredAtUtc >= start && m.OccurredAtUtc < end)
            .SumAsync(m => (decimal?)((-m.QtyBase) * m.UnitCost), cancellationToken) ?? 0m;

        // Net sales (ex-VAT) over the window — same basis as the VAT report's taxable sales.
        var netSales = await (
            from l in _db.OrderLines
            join o in _db.Orders on l.OrderId equals o.Id
            where o.IsPaid && o.Status != OrderStatus.Cancelled
                  && o.PaidAtUtc != null && o.PaidAtUtc >= start && o.PaidAtUtc < end
            select (decimal?)l.TaxableAmountSnapshot).SumAsync(cancellationToken) ?? 0m;

        var inventoryValue = await _db.InventoryItems
            .Where(i => i.IsActive)
            .SumAsync(i => (decimal?)(i.QtyOnHand * i.AvgCost), cancellationToken) ?? 0m;

        var foodCostPercent = netSales > 0m ? Math.Round(cogs / netSales * 100m, 2) : 0m;

        var currency = await _db.RestaurantBillingSettings
            .Select(s => s.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "Tk";

        return new FoodCostReportDto(netSales, cogs, foodCostPercent, wastage, inventoryValue, byCategory, currency);
    }
}
