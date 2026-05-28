using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.AccessRequests.Commands;
using Jewel.JPMS.Api.Features.AccessRequests.Queries;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.AccessRequests;

public static class AccessRequestsFeatureRegistration
{
    public static IServiceCollection AddAccessRequestsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListPendingAccessRequests, IReadOnlyList<AccessRequest>>, ListPendingAccessRequestsHandler>();

        services.AddScoped<ICommandHandler<SubmitAccessRequest, AccessRequest>, SubmitAccessRequestHandler>();
        services.AddScoped<SubmitAccessRequestAuthorisation>();
        services.AddScoped<SubmitAccessRequestValidation>();

        services.AddScoped<ICommandHandler<ResolveAccessRequest, Acknowledgement>, ResolveAccessRequestHandler>();
        services.AddScoped<ResolveAccessRequestAuthorisation>();
        services.AddScoped<ResolveAccessRequestValidation>();

        return services;
    }
}
