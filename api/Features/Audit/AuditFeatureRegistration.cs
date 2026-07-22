using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit.Queries;
using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Audit;

// The client-facing audit trail (pathway split): the append-only writer, the per-invocation actor
// holder the endpoint gates populate, and the register read.
public static class AuditFeatureRegistration
{
    public static IServiceCollection AddAuditFeature(this IServiceCollection services)
    {
        services.AddScoped<AuditActor>();
        services.AddScoped<AuditTrail>();
        services.AddScoped<IQueryHandler<ListAuditEvents, AuditEventsPage>, ListAuditEventsHandler>();
        return services;
    }
}
