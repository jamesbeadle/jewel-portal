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
            new QueryRoute("/api/cost-centers", _ => "/api/cost-centers"));
    }
}
