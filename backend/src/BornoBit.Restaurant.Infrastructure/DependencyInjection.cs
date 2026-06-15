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

        // Transient off the factory: each injection gets its own short-lived context so concurrent
        // operations on a long-lived Blazor circuit never share a DbContext (which would throw
        // "A second operation was started on this context instance"). Collaborators that must enlist
        // in a handler's unit of work take IAppDbContext as a METHOD parameter (see IDineInSessionResolver,
        // IStockConsumptionService) rather than injecting their own — so they share the caller's context.
        services.AddTransient<IAppDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddScoped<ISessionNumberGenerator, SessionNumberGenerator>();
        services.AddScoped<ITransactionNumberGenerator, TransactionNumberGenerator>();
        services.AddScoped<IJournalNumberGenerator, JournalNumberGenerator>();
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
        services.AddScoped<GeneralLedgerSeeder>();
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

        // Business-day boundaries against the configured local timezone (shared "Receipt:TimeZoneId").
        var businessTimeZoneId = configuration["Receipt:TimeZoneId"] ?? "Asia/Dhaka";
        services.AddSingleton<Application.Common.Time.IBusinessClock>(sp =>
            new Time.BusinessClock(sp.GetRequiredService<TimeProvider>(), businessTimeZoneId));

        return services;
    }
}
