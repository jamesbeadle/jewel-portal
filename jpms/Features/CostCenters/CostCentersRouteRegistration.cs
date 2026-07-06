using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.CostCenters;

public static class CostCentersRouteRegistration
{
    public static IServiceCollection AddCostCentersReadModels(this IServiceCollection services)
    {
        services.AddScoped<CostCentersReadModel>();
        return services;
    }

    public static void RegisterCostCentersRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListCostCenters, IReadOnlyList<CostCenter>>(
            new QueryRoute("/api/cost-centers",
                query => ((ListCostCenters)query).IncludeInactive
                    ? "/api/cost-centers?includeInactive=true"
                    : "/api/cost-centers"));

        commands.Register<AddCostCenter, CostCenter>(CommandRoute.Post("/api/cost-centers"));

        commands.Register<ReviseCostCenter, CostCenter>(
            new CommandRoute("PUT", "/api/cost-centers/{costCenterId}",
                command => $"/api/cost-centers/{((ReviseCostCenter)command).CostCenterId}"));
    }
}
