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
        services.AddScoped<RfiRegisterReadModel>();
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

        // Recipients preview: the exact To/CC/BCC set an issue or draft would use right now,
        // resolved through the same shared resolver as the send paths.
        queries.Register<ResolveRequestRecipients, RequestRecipientSet>(
            new QueryRoute("/api/requests/{requestId}/recipients",
                query => $"/api/requests/{((ResolveRequestRecipients)query).RequestId}/recipients"));

        queries.Register<ListRequestMessages, IReadOnlyList<RequestMessage>>(
            new QueryRoute("/api/requests/{requestId}/messages",
                query => $"/api/requests/{((ListRequestMessages)query).RequestId}/messages"));

        // Full body of one conversation email, fetched on demand when the reader expands it (the
        // conversation list only carries the short preview). Message ids go in the query string —
        // Graph ids contain path-unsafe chars.
        queries.Register<GetRequestEmailDetail, MailboxMessageDetail>(
            new QueryRoute("/api/requests/{requestId}/messages/email-detail",
                query =>
                {
                    var q = (GetRequestEmailDetail)query;
                    return $"/api/requests/{q.RequestId}/messages/email-detail"
                        + $"?id={Uri.EscapeDataString(q.MessageId)}&imid={Uri.EscapeDataString(q.InternetMessageId ?? string.Empty)}";
                }));

        queries.Register<ListUnassignedRequests, IReadOnlyList<Request>>(
            new QueryRoute("/api/requests/unassigned",
                _ => "/api/requests/unassigned"));

        // Cross-project RFI dashboard: every RFI on every live project in one register. The route
        // sits under /rfis (not /requests/rfis) so it can never be shadowed by "requests/{requestId}".
        queries.Register<ListRfisAcrossProjects, IReadOnlyList<Request>>(
            new QueryRoute("/api/rfis",
                _ => "/api/rfis"));

        // Live-read triage: read the Inbox (queue) and General (discarded) folders straight from the
        // mailbox. Message ids go in the query string, not the path (Graph ids contain path-unsafe chars).
        queries.Register<ListInboxMessages, MailboxPage>(
            new QueryRoute("/api/mailbox/inbox",
                query =>
                {
                    var q = (ListInboxMessages)query;
                    return $"/api/mailbox/inbox?cursor={Uri.EscapeDataString(q.Cursor ?? string.Empty)}&take={q.Take}";
                }));

        queries.Register<ListDiscardedMessages, MailboxPage>(
            new QueryRoute("/api/mailbox/discarded",
                query =>
                {
                    var q = (ListDiscardedMessages)query;
                    return $"/api/mailbox/discarded?cursor={Uri.EscapeDataString(q.Cursor ?? string.Empty)}&take={q.Take}";
                }));

        queries.Register<ListTaggedMessages, MailboxPage>(
            new QueryRoute("/api/mailbox/tagged",
                query =>
                {
                    var q = (ListTaggedMessages)query;
                    var tags = q.Tags is null ? string.Empty : string.Join(",", q.Tags);
                    return $"/api/mailbox/tagged?cursor={Uri.EscapeDataString(q.Cursor ?? string.Empty)}&take={q.Take}&tags={Uri.EscapeDataString(tags)}";
                }));

        // An email's whole thread (every Inbox message sharing its Graph conversation id), for the
        // triage detail pane's thread panel. The conversation id goes in the query string too.
        queries.Register<ListConversationMessages, MailboxPage>(
            new QueryRoute("/api/mailbox/conversation",
                query => $"/api/mailbox/conversation?id={Uri.EscapeDataString(((ListConversationMessages)query).ConversationId)}"));

        queries.Register<GetMailboxMessageDetail, MailboxMessageDetail>(
            new QueryRoute("/api/mailbox/message/detail",
                query =>
                {
                    var q = (GetMailboxMessageDetail)query;
                    return $"/api/mailbox/message/detail?id={Uri.EscapeDataString(q.MessageId)}&imid={Uri.EscapeDataString(q.InternetMessageId ?? string.Empty)}";
                }));

        // AI-assisted triage: Claude reads the email's thread and recommends the next action. The
        // subject/from travel along so the server can describe the email when it has no thread.
        queries.Register<RecommendTriageAction, TriageRecommendation>(
            new QueryRoute("/api/mailbox/message/recommend",
                query =>
                {
                    var q = (RecommendTriageAction)query;
                    return "/api/mailbox/message/recommend"
                        + $"?id={Uri.EscapeDataString(q.MessageId)}"
                        + $"&imid={Uri.EscapeDataString(q.InternetMessageId ?? string.Empty)}"
                        + $"&cid={Uri.EscapeDataString(q.ConversationId ?? string.Empty)}"
                        + $"&subject={Uri.EscapeDataString(q.Subject ?? string.Empty)}"
                        + $"&from={Uri.EscapeDataString(q.FromEmail ?? string.Empty)}"
                        + $"&fromName={Uri.EscapeDataString(q.FromName ?? string.Empty)}";
                }));

        commands.Register<RaiseRequest, Request>(
            new CommandRoute("POST", "/api/projects/{projectId}/requests",
                command => $"/api/projects/{((RaiseRequest)command).ProjectId}/requests"));

        commands.Register<UpdateRequestDetails, Request>(
            new CommandRoute("PUT", "/api/requests/{requestId}",
                command => $"/api/requests/{((UpdateRequestDetails)command).RequestId}"));

        // The structured body of the official document (itemised queries + narrative sections).
        commands.Register<UpdateRequestForm, Request>(
            new CommandRoute("PUT", "/api/requests/{requestId}/form",
                command => $"/api/requests/{((UpdateRequestForm)command).RequestId}/form"));

        // Stage the outbound email: an Outlook draft in the projects mailbox with the PDF attached.
        commands.Register<PrepareRequestEmailDraft, RequestEmailDraft>(
            new CommandRoute("POST", "/api/requests/{requestId}/email-draft",
                command => $"/api/requests/{((PrepareRequestEmailDraft)command).RequestId}/email-draft"));

        // Stage the outbound email as a REPLY inside an existing conversation thread: an Outlook
        // draft replying to a linked email, official PDF attached. Nothing is sent from here.
        commands.Register<PrepareRequestReplyDraft, RequestEmailDraft>(
            new CommandRoute("POST", "/api/requests/{requestId}/email-draft/reply",
                command => $"/api/requests/{((PrepareRequestReplyDraft)command).RequestId}/email-draft/reply"));

        // Bulk-stage outbound emails: one Outlook draft per request id in the body. Partial
        // success is reported per request; nothing is sent from here.
        commands.Register<PrepareRequestEmailDrafts, RequestEmailDraftBatch>(
            new CommandRoute("POST", "/api/requests/email-drafts", _ => "/api/requests/email-drafts"));

        commands.Register<PostRequestMessage, RequestMessage>(
            new CommandRoute("POST", "/api/requests/{requestId}/messages",
                command => $"/api/requests/{((PostRequestMessage)command).RequestId}/messages"));

        // Pre-RFI merge: fold one General request into another. The survivor is the route's
        // request; the merged-away request id travels in the body.
        commands.Register<MergeRequests, Request>(
            new CommandRoute("POST", "/api/requests/{requestId}/merge",
                command => $"/api/requests/{((MergeRequests)command).SurvivorRequestId}/merge"));

        commands.Register<DeleteRequest, Acknowledgement>(
            new CommandRoute("DELETE", "/api/requests/{requestId}",
                command => $"/api/requests/{((DeleteRequest)command).RequestId}"));

        commands.Register<ReturnRequestToTriage, Acknowledgement>(
            new CommandRoute("POST", "/api/requests/{requestId}/return-to-triage",
                command => $"/api/requests/{((ReturnRequestToTriage)command).RequestId}/return-to-triage"));

        // Request ladder: General -> RFI -> (RFQ), plus linking a request to its party (a client
        // account, or an architect acting on a client's behalf).
        commands.Register<PromoteRequestToRfi, Request>(
            new CommandRoute("POST", "/api/requests/{requestId}/promote-to-rfi",
                command => $"/api/requests/{((PromoteRequestToRfi)command).RequestId}/promote-to-rfi"));

        commands.Register<EnableRfqOnRequest, Request>(
            new CommandRoute("POST", "/api/requests/{requestId}/enable-rfq",
                command => $"/api/requests/{((EnableRfqOnRequest)command).RequestId}/enable-rfq"));

        commands.Register<LinkRequestToParty, Request>(
            new CommandRoute("PUT", "/api/requests/{requestId}/party",
                command => $"/api/requests/{((LinkRequestToParty)command).RequestId}/party"));

        // Live-read triage moves: discard (Inbox -> General) and restore (General -> Inbox). The
        // message id + internetMessageId travel in the JSON body, so the route is static.
        commands.Register<DiscardMessage, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/discard", _ => "/api/mailbox/message/discard"));

        commands.Register<RestoreMessage, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/restore", _ => "/api/mailbox/message/restore"));

        commands.Register<RemoveTagFromMessage, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/remove-tag", _ => "/api/mailbox/message/remove-tag"));

        commands.Register<AssignMessageToRequest, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/assign", _ => "/api/mailbox/message/assign"));

        commands.Register<CreateRequestFromMessage, Request>(
            new CommandRoute("POST", "/api/mailbox/message/create-request", _ => "/api/mailbox/message/create-request"));

        // Triage "Reply in thread": an Outlook reply draft staged on the email (projects mailbox,
        // thread quoted behind it) plus a background General request created from the same email.
        commands.Register<ReplyInThreadFromMessage, ReplyInThreadOutcome>(
            new CommandRoute("POST", "/api/mailbox/message/reply-in-thread", _ => "/api/mailbox/message/reply-in-thread"));
    }
}
