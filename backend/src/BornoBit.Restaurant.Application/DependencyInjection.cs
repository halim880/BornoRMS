using BornoBit.Restaurant.Application.Common.Behaviors;
using BornoBit.Restaurant.Application.Inventory.Consumption;
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

        return services;
    }
}
