using Microsoft.Extensions.DependencyInjection;

namespace BornoBit.Restaurant.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        services.AddScoped<IReportRenderer, QuestPdfReportRenderer>();
        return services;
    }
}
