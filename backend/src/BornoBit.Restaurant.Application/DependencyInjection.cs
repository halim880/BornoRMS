using BornoBit.Restaurant.Application.Common.Behaviors;
using BornoBit.Restaurant.Application.Inventory.Consumption;
using BornoBit.Restaurant.Application.Ordering.Common;
using BornoBit.Restaurant.Application.Ordering.Printing;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BornoBit.Restaurant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        var assemblies = new[] { Assembly.GetExecutingAssembly() }.Concat(additionalAssemblies).ToArray();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        foreach (var asm in assemblies)
            services.AddValidatorsFromAssembly(asm);

        // Unified stock pipeline (stateless — shares the caller's IAppDbContext per method call).
        services.AddScoped<IStockConsumptionService, StockConsumptionService>();

        // Direct multi-line GL poster for accrual entries (AP, VAT, depreciation, payroll, close).
        // Stateless like the stock pipeline — shares the caller's IAppDbContext per method call.
        services.AddScoped<Accounting.Posting.IGeneralLedgerService, Accounting.Posting.GeneralLedgerService>();

        // Single source of truth for dine-in table occupancy (shared by waiter + POS order flows).
        services.AddScoped<IDineInSessionResolver, DineInSessionResolver>();

        // Default no-op KOT transport. The Web host overrides this with the real print-agent sender;
        // the API host keeps the no-op (no print agent there).
        services.AddScoped<IKitchenTicketSender, NullKitchenTicketSender>();

        return services;
    }
}
