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

        queries.Register<ListUnassignedRequests, IReadOnlyList<Request>>(
            new QueryRoute("/api/requests/unassigned",
                _ => "/api/requests/unassigned"));

        // Live-read triage: read the Inbox (queue) and General (discarded) folders straight from the
        // mailbox. Message ids go in the query string, not the path (Graph ids contain path-unsafe chars).
        queries.Register<ListInboxMessages, PagedResult<MailboxMessage>>(
            new QueryRoute("/api/mailbox/inbox",
                query =>
                {
                    var q = (ListInboxMessages)query;
                    return $"/api/mailbox/inbox?skip={q.Skip}&take={q.Take}";
                }));

        queries.Register<ListDiscardedMessages, PagedResult<MailboxMessage>>(
            new QueryRoute("/api/mailbox/discarded",
                query =>
                {
                    var q = (ListDiscardedMessages)query;
                    return $"/api/mailbox/discarded?skip={q.Skip}&take={q.Take}";
                }));

        queries.Register<GetMailboxMessageDetail, MailboxMessageDetail>(
            new QueryRoute("/api/mailbox/message/detail",
                query =>
                {
                    var q = (GetMailboxMessageDetail)query;
                    return $"/api/mailbox/message/detail?id={Uri.EscapeDataString(q.MessageId)}&imid={Uri.EscapeDataString(q.InternetMessageId ?? string.Empty)}";
                }));

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

        // Live-read triage moves: discard (Inbox -> General) and restore (General -> Inbox). The
        // message id + internetMessageId travel in the JSON body, so the route is static.
        commands.Register<DiscardMessage, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/discard", _ => "/api/mailbox/message/discard"));

        commands.Register<RestoreMessage, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/restore", _ => "/api/mailbox/message/restore"));

        commands.Register<AssignMessageToRequest, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/assign", _ => "/api/mailbox/message/assign"));

        commands.Register<CreateRequestFromMessage, Request>(
            new CommandRoute("POST", "/api/mailbox/message/create-request", _ => "/api/mailbox/message/create-request"));
    }
}
