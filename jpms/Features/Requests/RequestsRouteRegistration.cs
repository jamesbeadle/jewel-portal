using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Requests;

public static class RequestsRouteRegistration
{
    public static IServiceCollection AddRequestsReadModels(this IServiceCollection services)
    {
        services.AddScoped<RequestsReadModel>();
        return services;
    }

    public static void RegisterRequestsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListRequestsForProject, IReadOnlyList<Request>>(
            new QueryRoute("/api/projects/{projectId}/requests",
                query => $"/api/projects/{((ListRequestsForProject)query).ProjectId}/requests"));

        queries.Register<GetRequestById, Request?>(
            new QueryRoute("/api/requests/{requestId}",
                query => $"/api/requests/{((GetRequestById)query).RequestId}"));

        queries.Register<ListRequestMessages, IReadOnlyList<RequestMessage>>(
            new QueryRoute("/api/requests/{requestId}/messages",
                query => $"/api/requests/{((ListRequestMessages)query).RequestId}/messages"));

        queries.Register<ListOpenIntake, IReadOnlyList<IntakeEmail>>(
            new QueryRoute("/api/intake",
                _ => "/api/intake"));

        queries.Register<GetIntakeEmailDetail, IntakeEmailDetail>(
            new QueryRoute("/api/intake/{intakeId}/detail",
                query => $"/api/intake/{((GetIntakeEmailDetail)query).IntakeId}/detail"));

        queries.Register<SuggestRequestFromIntake, RequestSuggestion>(
            new QueryRoute("/api/intake/{intakeId}/suggest",
                query => $"/api/intake/{((SuggestRequestFromIntake)query).IntakeId}/suggest"));

        commands.Register<RaiseRequest, Request>(
            new CommandRoute("POST", "/api/projects/{projectId}/requests",
                command => $"/api/projects/{((RaiseRequest)command).ProjectId}/requests"));

        commands.Register<UpdateRequestDetails, Request>(
            new CommandRoute("PUT", "/api/requests/{requestId}",
                command => $"/api/requests/{((UpdateRequestDetails)command).RequestId}"));

        commands.Register<PostRequestMessage, RequestMessage>(
            new CommandRoute("POST", "/api/requests/{requestId}/messages",
                command => $"/api/requests/{((PostRequestMessage)command).RequestId}/messages"));

        commands.Register<DeleteRequest, Acknowledgement>(
            new CommandRoute("DELETE", "/api/requests/{requestId}",
                command => $"/api/requests/{((DeleteRequest)command).RequestId}"));

        commands.Register<ReturnRequestToTriage, Acknowledgement>(
            new CommandRoute("POST", "/api/requests/{requestId}/return-to-triage",
                command => $"/api/requests/{((ReturnRequestToTriage)command).RequestId}/return-to-triage"));

        commands.Register<ClaimIntakeEmail, IntakeEmail>(
            new CommandRoute("POST", "/api/intake/{intakeId}/claim",
                command => $"/api/intake/{((ClaimIntakeEmail)command).IntakeId}/claim"));

        commands.Register<DiscardIntakeEmail, IntakeEmail>(
            new CommandRoute("POST", "/api/intake/{intakeId}/discard",
                command => $"/api/intake/{((DiscardIntakeEmail)command).IntakeId}/discard"));

        commands.Register<LinkIntakeToRequest, IntakeEmail>(
            new CommandRoute("POST", "/api/intake/{intakeId}/link",
                command => $"/api/intake/{((LinkIntakeToRequest)command).IntakeId}/link"));

        commands.Register<CreateRequestFromIntake, Request>(
            new CommandRoute("POST", "/api/intake/{intakeId}/create-request",
                command => $"/api/intake/{((CreateRequestFromIntake)command).IntakeId}/create-request"));
    }
}
