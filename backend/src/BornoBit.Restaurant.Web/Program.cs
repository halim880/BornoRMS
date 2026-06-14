using BornoBit.Restaurant.Application;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Infrastructure;
using BornoBit.Restaurant.Infrastructure.Persistence;
using BornoBit.Restaurant.Infrastructure.Persistence.Seeding;
using BornoBit.Restaurant.Reporting;
using BornoBit.Restaurant.Web.Components;
using BornoBit.Restaurant.Web.Endpoints;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).ReadFrom.Services(services));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<BornoBit.Restaurant.Web.Components.BornoUi.Dialog.IBoDialogService, BornoBit.Restaurant.Web.Components.BornoUi.Dialog.BoDialogService>();
builder.Services.AddScoped<BornoBit.Restaurant.Web.Components.BornoUi.Toast.IBoToastService, BornoBit.Restaurant.Web.Components.BornoUi.Toast.BoToastService>();
builder.Services.AddScoped<BornoBit.Restaurant.Web.Services.IImageUploadService, BornoBit.Restaurant.Web.Services.ImageUploadService>();

builder.Services.AddApplication(typeof(BornoBit.Restaurant.Infrastructure.DependencyInjection).Assembly);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddReporting();
builder.Services.Configure<BornoBit.Restaurant.Reporting.Models.ReceiptBranding>(
    builder.Configuration.GetSection(BornoBit.Restaurant.Reporting.Models.ReceiptBranding.SectionName));

// Local print agent: HTTP push to the agent's API, or SignalR hub the agent dials into.
builder.Services.AddSignalR();
builder.Services.Configure<BornoBit.Restaurant.Web.Services.Printing.PrintAgentOptions>(
    builder.Configuration.GetSection(BornoBit.Restaurant.Web.Services.Printing.PrintAgentOptions.SectionName));
builder.Services.AddHttpClient("PrintAgent", (sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BornoBit.Restaurant.Web.Services.Printing.PrintAgentOptions>>().Value;
    if (Uri.TryCreate(opts.BaseUrl, UriKind.Absolute, out var baseUrl))
        client.BaseAddress = baseUrl;
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
        client.DefaultRequestHeaders.Add("X-Api-Key", opts.ApiKey);
});
builder.Services.AddScoped<BornoBit.Restaurant.Web.Services.Printing.IReceiptPrintService,
    BornoBit.Restaurant.Web.Services.Printing.ReceiptPrintService>();

// Operations dashboard real-time: an in-process notifier for Web-side actions, plus a poller that
// bridges API-process changes (customer QR orders/requests) onto the dashboard's SignalR hub.
builder.Services.AddSingleton<BornoBit.Restaurant.Web.Services.Dashboard.IDashboardNotifier,
    BornoBit.Restaurant.Web.Services.Dashboard.DashboardNotifier>();
builder.Services.AddHostedService<BornoBit.Restaurant.Web.Services.Dashboard.DashboardPollingService>();
builder.Services.AddHostedService<BornoBit.Restaurant.Web.Services.Stock.StockSyncRetryService>();

// Payment provider adapter — demo/mock implementation. Swap for a real gateway in production (see README).
builder.Services.AddSingleton<BornoBit.Restaurant.Application.Ordering.Payments.IPaymentGateway,
    BornoBit.Restaurant.Web.Services.Payments.MockPaymentGateway>();

// Instant manager-override authorization (large discounts / voids / refunds at the till).
builder.Services.AddScoped<BornoBit.Restaurant.Application.Common.Security.IManagerApprovalService,
    BornoBit.Restaurant.Infrastructure.Identity.ManagerApprovalService>();

builder.Services.Configure<BornoBit.Restaurant.Web.Services.CustomerSiteOptions>(
    builder.Configuration.GetSection(BornoBit.Restaurant.Web.Services.CustomerSiteOptions.SectionName));
builder.Services.AddSingleton<BornoBit.Restaurant.Web.Services.TableQrService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/forbidden";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Staff", p => p.RequireRole(Roles.StaffOrderManagers.ToArray()));
    options.AddPolicy("SuperAdmin", p => p.RequireRole(Roles.SuperAdmin));
    options.AddPolicy("Admin", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin));
    options.AddPolicy("Inventory", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("Store", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("Reports", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("Kitchen", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Chef));

    // Waiter operations console: floor access for service staff; sensitive billing actions restricted.
    options.AddPolicy("WaiterFloor", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Waiter));
    options.AddPolicy("CanDiscount", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("CanVoid", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("CanSettle", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Cashier));
    options.AddPolicy("CanCloseSession", p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Cashier));
});
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<RoleSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<SuperAdminSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<MenuSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<TableSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<KitchenStationSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<CustomerSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<TenantSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<InventorySeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<UnitSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<StockSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<RecipeSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<StoreUnitSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<AccountingSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<BillingSettingsSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<AppMenuSeeder>().SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Startup migration/seeding failed.");
    }
}

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    }
});
app.MapAccountEndpoints();
app.MapReportEndpoints();
// Agent connections authenticate via X-Agent-Key inside the hub — no cookie policy here.
app.MapHub<BornoBit.Restaurant.Web.Hubs.PrintHub>("/hubs/print");
// Dashboard tick channel — emits only content-free "changed" signals; data is fetched per-circuit.
app.MapHub<BornoBit.Restaurant.Web.Hubs.DashboardHub>("/hubs/dashboard");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
