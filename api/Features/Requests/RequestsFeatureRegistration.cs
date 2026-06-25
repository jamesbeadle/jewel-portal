using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Api.Features.Requests.Queries;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Requests;

public static class RequestsFeatureRegistration
{
    public static IServiceCollection AddRequestsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>>, ListRequestsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetRequestById, Request?>, GetRequestByIdHandler>();

        services.AddScoped<ICommandHandler<RaiseRequest, Request>, RaiseRequestHandler>();
        services.AddScoped<RaiseRequestAuthorisation>();
        services.AddScoped<RaiseRequestValidation>();

        services.AddScoped<ICommandHandler<UpdateRequestDetails, Request>, UpdateRequestDetailsHandler>();
        services.AddScoped<UpdateRequestDetailsAuthorisation>();
        services.AddScoped<UpdateRequestDetailsValidation>();

        return services;
    }
}
