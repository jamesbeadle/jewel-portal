using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Portal.Commands;
using Jewel.JPMS.Api.Features.Portal.Queries;
using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Portal;

/// <summary>
/// The subcontractor portal: /portal/my/* endpoints where the caller is an external subcontractor
/// contact and every read/write is scoped to their own SubcontractorId (Gates/SubcontractorScope).
/// See docs/06-backlog/subcontractor-crm-scope.md.
/// </summary>
public static class PortalFeatureRegistration
{
    public static IServiceCollection AddPortalFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetMyPortalRecord, SubcontractorPortalRecord?>, GetMyPortalRecordHandler>();
        services.AddScoped<IQueryHandler<ListMyWorkOrders, IReadOnlyList<PortalWorkOrder>>, ListMyWorkOrdersHandler>();
        services.AddScoped<ICommandHandler<RaiseMyVariationRequest, SubcontractorVariationRequest>, RaiseMyVariationRequestHandler>();
        return services;
    }
}
