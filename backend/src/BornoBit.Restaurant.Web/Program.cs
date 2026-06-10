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
        await scope.ServiceProvider.GetRequiredService<CustomerSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<TenantSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<InventorySeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<UnitSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<StockSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<StoreUnitSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<ChartOfAccountsSeeder>().SeedAsync();
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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
