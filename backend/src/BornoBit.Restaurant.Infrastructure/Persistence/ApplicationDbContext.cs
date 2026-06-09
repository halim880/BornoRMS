using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Domain.Catalog;
using BornoBit.Restaurant.Domain.Common;
using BornoBit.Restaurant.Domain.Customers;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Domain.Menus;
using BornoBit.Restaurant.Domain.Numbering;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Domain.Tenants;
using BornoBit.Restaurant.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace BornoBit.Restaurant.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IAppDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerOtp> CustomerOtps => Set<CustomerOtp>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppMenu> AppMenus => Set<AppMenu>();
    public DbSet<AppMenuRolePermission> AppMenuRolePermissions => Set<AppMenuRolePermission>();
    public DbSet<NumberingScope> NumberingScopes => Set<NumberingScope>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyGlobalFilters(builder);
    }

    private static void ApplyGlobalFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(ISoftDelete).IsAssignableFrom(clrType)) continue;

            var parameter = Expression.Parameter(clrType, "e");
            var isDeletedProp = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Equal(isDeletedProp, Expression.Constant(false));
            var lambda = Expression.Lambda(notDeleted, parameter);
            builder.Entity(clrType).HasQueryFilter(lambda);
        }
    }
}
