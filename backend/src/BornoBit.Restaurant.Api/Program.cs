using BornoBit.Restaurant.Api.Endpoints;
using BornoBit.Restaurant.Application;
using BornoBit.Restaurant.Application.Customers.Portal;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Infrastructure;
using BornoBit.Restaurant.Infrastructure.Persistence.Seeding;
using BornoBit.Restaurant.Reporting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).ReadFrom.Services(services));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddReporting();

builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
builder.Services.Configure<BornoBit.Restaurant.Reporting.Models.ReceiptBranding>(
    builder.Configuration.GetSection(BornoBit.Restaurant.Reporting.Models.ReceiptBranding.SectionName));

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");

var staffAudience = jwtSection["Audience"] ?? "BornoBit.Restaurant.Staff";
var customerAudience = jwtSection["CustomerAudience"] ?? "BornoBit.Restaurant.Customer";
var validAudiences = new[] { staffAudience, customerAudience };

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudiences = validAudiences,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Customer", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("typ", "customer"));

    options.AddPolicy("Staff", policy =>
        policy.RequireAuthenticatedUser().RequireRole(Roles.StaffOrderManagers.ToArray()));

    // Waiter mobile app: floor access for service staff; closing a session is a billing-sensitive action.
    options.AddPolicy("WaiterFloor", policy =>
        policy.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Waiter));

    options.AddPolicy("CanCloseSession", policy =>
        policy.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Cashier));
});

var customerOrigins = builder.Configuration.GetSection("Cors:CustomerOrigins").Get<string[]>() ?? Array.Empty<string>();
var adminOrigins = builder.Configuration.GetSection("Cors:AdminOrigins").Get<string[]>() ?? Array.Empty<string>();
var allOrigins = customerOrigins.Concat(adminOrigins).Distinct().ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontends", policy =>
        policy.WithOrigins(allOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<BornoBit.Restaurant.Infrastructure.Persistence.ApplicationDbContext>();
        await db.Database.MigrateAsync();

        await scope.ServiceProvider.GetRequiredService<RoleSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<SuperAdminSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<StaffUserSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<MenuSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<TableSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<KitchenStationSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<CustomerSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<InventorySeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<UnitSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<StockSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<RecipeSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<StoreUnitSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<AccountingSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<GeneralLedgerSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<BillingSettingsSeeder>().SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Startup migration/seeding failed.");
    }
}

app.UseSerilogRequestLogging();

// Serve product images (wwwroot/img/products) so the mobile waiter app can load them.
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontends");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapMenuEndpoints();
app.MapTableEndpoints();
app.MapCustomerAuthEndpoints();
app.MapStaffAuthEndpoints();
app.MapOrderEndpoints();
app.MapAdminOrderEndpoints();
app.MapCustomerRequestEndpoints();
app.MapReceiptEndpoints();
app.MapWaiterEndpoints();

app.MapGet("/", () => Results.Ok(new { app = "BornoBit.Restaurant.Api", version = "0.1.0" }));

app.Run();

public partial class Program { }
