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
        services.AddScoped<IJournalNumberGenerator, JournalNumberGenerator>();
        services.AddScoped<ISmsSender, StubSmsSender>();
        services.AddScoped<ICustomerTokenService, CustomerTokenService>();
        services.AddScoped<IStaffTokenService, StaffTokenService>();

        services.AddScoped<RoleSeeder>();
        services.AddScoped<SuperAdminSeeder>();
        services.AddScoped<MenuSeeder>();
        services.AddScoped<TableSeeder>();
        services.AddScoped<CustomerSeeder>();
        services.AddScoped<TenantSeeder>();
        services.AddScoped<AppMenuSeeder>();
        services.AddScoped<InventorySeeder>();
        services.AddScoped<UnitSeeder>();
        services.AddScoped<StockSeeder>();
        services.AddScoped<StoreUnitSeeder>();
        services.AddScoped<ChartOfAccountsSeeder>();

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

        return services;
    }
}
