using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Menus;
using BornoBit.Restaurant.Domain.Numbering;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace BornoBit.Restaurant.Application.Common.Persistence;

public interface IAppDbContext
{
    DbSet<MenuCategory> MenuCategories { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerOtp> CustomerOtps { get; }
    DbSet<RestaurantTable> RestaurantTables { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderLine> OrderLines { get; }

    DbSet<Tenant> Tenants { get; }
    DbSet<AppMenu> AppMenus { get; }
    DbSet<AppMenuRolePermission> AppMenuRolePermissions { get; }
    DbSet<NumberingScope> NumberingScopes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
