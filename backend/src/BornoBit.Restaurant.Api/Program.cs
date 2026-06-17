using BornoBit.Restaurant.Api.Endpoints;
using BornoBit.Restaurant.Application;
using BornoBit.Restaurant.Application.Customers.Portal;
using BornoBit.Restaurant.Domain.Identity;
using BornoBit.Restaurant.Infrastructure;
using BornoBit.Restaurant.Infrastructure.Persistence.Seeding;
using BornoBit.Restaurant.Reporting;
using Asp.Versioning;
using Asp.Versioning.Builder;
using BornoBit.Restaurant.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).ReadFrom.Services(services));

// Register Application handlers AND Infrastructure-resident handlers (Users/Roles management),
// mirroring the Web host, so the staff admin REST endpoints can resolve them.
builder.Services.AddApplication(typeof(BornoBit.Restaurant.Infrastructure.DependencyInjection).Assembly);
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

    // Staff-console parity policies (mirror BornoBit.Restaurant.Web) for the ported admin/back-office
    // REST endpoints consumed by the Flutter app.
    options.AddPolicy("SuperAdmin", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin));
    options.AddPolicy("Admin", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin));
    options.AddPolicy("Manager", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("Inventory", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("Store", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("Reports", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("Kitchen", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Chef));
    options.AddPolicy("CanDiscount", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("CanVoid", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy("CanSettle", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Cashier));
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

// URL-segment API versioning (/api/v1/...). New staff endpoints live under the versioned group;
// legacy customer/waiter routes stay unversioned for back-compat with the existing waiter app.
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

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
        await scope.ServiceProvider.GetRequiredService<StoreDepartmentSeeder>().SeedAsync();
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
    app.UseSwaggerUI(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
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

// Versioned staff API (/api/v1/...). The version set is shared by all v1 endpoint mappers.
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var apiV1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet)
    .HasApiVersion(new ApiVersion(1, 0));

apiV1.MapDashboardEndpoints();
apiV1.MapStaffMenuEndpoints();
apiV1.MapStaffOrderEndpoints();
apiV1.MapStaffPosEndpoints();

app.MapGet("/", () => Results.Ok(new { app = "BornoBit.Restaurant.Api", version = "0.1.0" }));

app.Run();

public partial class Program { }
