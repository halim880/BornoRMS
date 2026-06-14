using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Ordering;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Operations.Dashboard;

/// <summary>
/// Section-3 pie chart: paid revenue grouped by product category. Order lines snapshot a Product id in
/// <see cref="Domain.Ordering.OrderLine.MenuItemId"/> (both the customer and waiter flows store a Product id),
/// so we resolve the category via Product → ProductCategory; unmatched lines fall into "Other".
/// </summary>
public record GetSalesByCategoryQuery(DateTime From, DateTime To) : IRequest<IReadOnlyList<CategorySalesDto>>;

public record CategorySalesDto(string Category, decimal Revenue, int Quantity);

public class GetSalesByCategoryQueryHandler : IRequestHandler<GetSalesByCategoryQuery, IReadOnlyList<CategorySalesDto>>
{
    private readonly IAppDbContext _db;

    public GetSalesByCategoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategorySalesDto>> Handle(GetSalesByCategoryQuery request, CancellationToken cancellationToken)
    {
        var start = request.From.Date;
        var end = request.To.Date.AddDays(1);

        var lines = await _db.Orders
            .Where(o => o.IsPaid && o.Status != OrderStatus.Cancelled
                        && o.PaidAtUtc != null && o.PaidAtUtc >= start && o.PaidAtUtc < end)
            .SelectMany(o => o.Lines.Select(l => new
            {
                l.MenuItemId,
                l.Quantity,
                Revenue = l.UnitPriceSnapshot * l.Quantity
            }))
            .ToListAsync(cancellationToken);

        if (lines.Count == 0) return new List<CategorySalesDto>();

        var productIds = lines.Select(l => l.MenuItemId).Distinct().ToList();

        var categoryByProduct = await (
            from p in _db.Products
            join c in _db.ProductCategories on p.ProductCategoryId equals c.Id
            where productIds.Contains(p.Id)
            select new { p.Id, CategoryName = c.Name })
            .ToDictionaryAsync(x => x.Id, x => x.CategoryName, cancellationToken);

        return lines
            .GroupBy(l => categoryByProduct.TryGetValue(l.MenuItemId, out var name) ? name : "Other")
            .Select(g => new CategorySalesDto(g.Key, g.Sum(x => x.Revenue), g.Sum(x => x.Quantity)))
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }
}
