using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Rates;

public static class RatesRouteRegistration
{
    public static IServiceCollection AddRatesReadModels(this IServiceCollection services)
    {
        services.AddScoped<RateLibraryReadModel>();
        return services;
    }

    public static void RegisterRatesRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListRatesInLibrary, IReadOnlyList<Rate>>(QueryRoute.Static("/api/rates"));

        commands.Register<AddRate, Rate>(CommandRoute.Post("/api/rates"));

        commands.Register<ReviseRate, Rate>(
            new CommandRoute("PUT", "/api/rates/{rateId}",
                command => $"/api/rates/{((ReviseRate)command).RateId}"));
    }
}
