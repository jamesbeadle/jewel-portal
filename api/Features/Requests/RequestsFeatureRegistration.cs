using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Api.Features.Requests.Queries;
using Jewel.JPMS.Contracts.Cqrs;
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
        services.AddScoped<IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>>, ListRequestMessagesHandler>();
        services.AddScoped<IQueryHandler<ListOpenIntake, IReadOnlyList<IntakeEmail>>, ListOpenIntakeHandler>();

        services.AddScoped<ICommandHandler<RaiseRequest, Request>, RaiseRequestHandler>();
        services.AddScoped<RaiseRequestAuthorisation>();
        services.AddScoped<RaiseRequestValidation>();

        services.AddScoped<ICommandHandler<UpdateRequestDetails, Request>, UpdateRequestDetailsHandler>();
        services.AddScoped<UpdateRequestDetailsAuthorisation>();
        services.AddScoped<UpdateRequestDetailsValidation>();

        services.AddScoped<ICommandHandler<PostRequestMessage, RequestMessage>, PostRequestMessageHandler>();
        services.AddScoped<PostRequestMessageAuthorisation>();
        services.AddScoped<PostRequestMessageValidation>();

        services.AddScoped<ICommandHandler<DeleteRequest, Acknowledgement>, DeleteRequestHandler>();
        services.AddScoped<DeleteRequestAuthorisation>();
        services.AddScoped<DeleteRequestValidation>();

        services.AddScoped<ICommandHandler<ClaimIntakeEmail, IntakeEmail>, ClaimIntakeEmailHandler>();
        services.AddScoped<ClaimIntakeEmailAuthorisation>();
        services.AddScoped<ClaimIntakeEmailValidation>();

        services.AddScoped<ICommandHandler<DiscardIntakeEmail, IntakeEmail>, DiscardIntakeEmailHandler>();
        services.AddScoped<DiscardIntakeEmailAuthorisation>();
        services.AddScoped<DiscardIntakeEmailValidation>();

        services.AddScoped<ICommandHandler<LinkIntakeToRequest, IntakeEmail>, LinkIntakeToRequestHandler>();
        services.AddScoped<LinkIntakeToRequestAuthorisation>();
        services.AddScoped<LinkIntakeToRequestValidation>();

        services.AddScoped<ICommandHandler<CreateRequestFromIntake, Request>, CreateRequestFromIntakeHandler>();
        services.AddScoped<CreateRequestFromIntakeAuthorisation>();
        services.AddScoped<CreateRequestFromIntakeValidation>();

        return services;
    }
}
