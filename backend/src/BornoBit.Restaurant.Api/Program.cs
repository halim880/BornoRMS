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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).ReadFrom.Services(services));

// Register Application handlers AND Infrastructure-resident handlers (Users/Roles management),
// mirroring the Web host, so the staff admin REST endpoints can resolve them.
builder.Services.AddApplication(typeof(BornoBit.Restaurant.Infrastructure.DependencyInjection).Assembly);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddReporting();

// Posts each business day's cash-counter takings to the GL automatically (day-end), so the books never
// silently drift behind the till. Idempotent — safe alongside the manual Cash Counter import.
builder.Services.AddHostedService<BornoBit.Restaurant.Api.Services.CashCounterImportService>();

// Real-time tick channel for the Flutter staff console (POS / KDS / waiter / dashboard). The hub
// only emits content-free "changed" signals; clients re-fetch via their existing authenticated REST
// queries. Polling stays as a fallback when the socket is down.
builder.Services.AddSignalR();
builder.Services.AddSingleton<BornoBit.Restaurant.Api.Services.ILiveNotifier,
    BornoBit.Restaurant.Api.Services.LiveNotifier>();

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

        // WebSockets can't send an Authorization header, so SignalR clients pass the JWT via the
        // access_token query string. Lift it onto the request only for the live hub handshake.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/live"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
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
    options.AddPolicy("Delivery", p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager, Roles.Cashier));
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

// Rate limiting. The "auth" policy is a strict brute-force guard on the credential endpoints
// (login/refresh); a lenient per-IP global limiter is a backstop against runaway clients. The
// global limiter is only enforced when the middleware is added (gated by RateLimiting:Enabled),
// so the named policy is always registered safely.
var rlSection = builder.Configuration.GetSection("RateLimiting");
var rateLimitingEnabled = !bool.TryParse(rlSection["Enabled"], out var rlEnabled) || rlEnabled;
var globalPerMinute = int.TryParse(rlSection["GlobalPermitPerMinute"], out var gpm) && gpm > 0 ? gpm : 1000;
var authPerMinute = int.TryParse(rlSection["AuthPermitPerMinute"], out var apm) && apm > 0 ? apm : 10;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = authPerMinute;
        o.QueueLimit = 0;
    });

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
    {
        // The live SignalR hub holds a long-lived connection — exempt it.
        if (http.Request.Path.StartsWithSegments("/hubs"))
            return RateLimitPartition.GetNoLimiter("hub");

        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = globalPerMinute,
            QueueLimit = 0
        });
    });
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
        await scope.ServiceProvider.GetRequiredService<KitchenSeeder>().SeedAsync();
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
if (rateLimitingEnabled)
{
    app.UseRateLimiter();
}
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

// Real-time tick channel — emits only content-free "changed" signals (auth = staff JWT).
app.MapHub<BornoBit.Restaurant.Api.Hubs.LiveHub>("/hubs/live").RequireCors("Frontends");

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
apiV1.MapKitchenEndpoints();
apiV1.MapCatalogAdminEndpoints();
apiV1.MapReportsEndpoints();
apiV1.MapStockEndpoints();
apiV1.MapAccountsEndpoints();
apiV1.MapStoreEndpoints();
apiV1.MapAdminEndpoints();
apiV1.MapSettingsEndpoints();
apiV1.MapLogisticsEndpoints();

app.MapGet("/", () => Results.Ok(new { app = "BornoBit.Restaurant.Api", version = "0.1.0" }));

app.Run();

public partial class Program { }
