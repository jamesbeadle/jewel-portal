using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.CostCenters.Commands;
using Jewel.JPMS.Api.Features.CostCenters.Queries;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.CostCenters;

public static class CostCentersFeatureRegistration
{
    public static IServiceCollection AddCostCentersFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListCostCenters, IReadOnlyList<CostCenter>>, ListCostCentersHandler>();

        services.AddScoped<ICommandHandler<AddCostCenter, CostCenter>, AddCostCenterHandler>();
        services.AddScoped<AddCostCenterAuthorisation>();
        services.AddScoped<AddCostCenterValidation>();

        services.AddScoped<ICommandHandler<ReviseCostCenter, CostCenter>, ReviseCostCenterHandler>();
        services.AddScoped<ReviseCostCenterAuthorisation>();
        services.AddScoped<ReviseCostCenterValidation>();

        return services;
    }
}
