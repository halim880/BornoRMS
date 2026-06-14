using BornoBit.Restaurant.Application.Common.Identity;
using BornoBit.Restaurant.Application.Common.Numbering;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Sms;
using BornoBit.Restaurant.Infrastructure.Identity;
using BornoBit.Restaurant.Infrastructure.Numbering;
using BornoBit.Restaurant.Infrastructure.Persistence;
using BornoBit.Restaurant.Infrastructure.Persistence.Interceptors;
using BornoBit.Restaurant.Infrastructure.Persistence.Seeding;
using BornoBit.Restaurant.Infrastructure.Sms;
using BornoBit.Restaurant.Shared.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BornoBit.Restaurant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddScoped<AuditableEntityInterceptor>();

        var connectionString = configuration.GetConnectionString("AppDb")
            ?? throw new InvalidOperationException("Connection string 'AppDb' is missing.");

        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        }, ServiceLifetime.Scoped);

        services.AddScoped<ApplicationDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        services.AddTransient<IAppDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddScoped<ISessionNumberGenerator, SessionNumberGenerator>();
        services.AddScoped<ITransactionNumberGenerator, TransactionNumberGenerator>();
        services.AddScoped<IDrawerNumberGenerator, DrawerNumberGenerator>();
        services.AddScoped<ISmsSender, StubSmsSender>();
        services.AddScoped<ICustomerTokenService, CustomerTokenService>();
        services.AddScoped<IStaffTokenService, StaffTokenService>();

        services.AddScoped<RoleSeeder>();
        services.AddScoped<SuperAdminSeeder>();
        services.AddScoped<MenuSeeder>();
        services.AddScoped<TableSeeder>();
        services.AddScoped<KitchenStationSeeder>();
        services.AddScoped<CustomerSeeder>();
        services.AddScoped<TenantSeeder>();
        services.AddScoped<AppMenuSeeder>();
        services.AddScoped<InventorySeeder>();
        services.AddScoped<UnitSeeder>();
        services.AddScoped<StockSeeder>();
        services.AddScoped<RecipeSeeder>();
        services.AddScoped<StoreUnitSeeder>();
        services.AddScoped<AccountingSeeder>();
        services.AddScoped<BillingSettingsSeeder>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Payment provider adapter — demo/mock implementation. Swap for a real gateway in production (see README).
        services.AddSingleton<Application.Ordering.Payments.IPaymentGateway, Payments.MockPaymentGateway>();

        // Instant manager-override authorization (large discounts / voids / refunds at the till).
        services.AddScoped<Application.Common.Security.IManagerApprovalService, ManagerApprovalService>();

        return services;
    }
}
