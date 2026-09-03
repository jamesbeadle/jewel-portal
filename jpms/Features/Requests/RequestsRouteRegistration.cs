
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

        commands.Register<CloseRequest, RequestCloseOutcome>(
            new CommandRoute("POST", "/api/requests/{requestId}/close",
                command => $"/api/requests/{((CloseRequest)command).RequestId}/close"));

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
            new QueryRoute("/api/requests-unassigned",
                _ => "/api/requests-unassigned"));

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
                    return $"/api/mailbox/inbox?cursor={Uri.EscapeDataString(q.Cursor ?? string.Empty)}&take={q.Take}&newestFirst={(q.NewestFirst ? "true" : "false")}";
                }));

        queries.Register<ListDiscardedMessages, MailboxPage>(
            new QueryRoute("/api/mailbox/discarded",
                query =>
                {
                    var q = (ListDiscardedMessages)query;
                    return $"/api/mailbox/discarded?cursor={Uri.EscapeDataString(q.Cursor ?? string.Empty)}&take={q.Take}&newestFirst={(q.NewestFirst ? "true" : "false")}";
                }));

        queries.Register<ListTaggedMessages, MailboxPage>(
            new QueryRoute("/api/mailbox/tagged",
                query =>
                {
                    var q = (ListTaggedMessages)query;
                    var tags = q.Tags is null ? string.Empty : string.Join(",", q.Tags);
                    return $"/api/mailbox/tagged?cursor={Uri.EscapeDataString(q.Cursor ?? string.Empty)}&take={q.Take}&tags={Uri.EscapeDataString(tags)}&newestFirst={(q.NewestFirst ? "true" : "false")}";
                }));

        // An email's whole thread (every Inbox message sharing its Graph conversation id), for the
        // triage detail pane's thread panel. The conversation id goes in the query string too.
        queries.Register<ListConversationMessages, MailboxPage>(
            new QueryRoute("/api/mailbox/conversation",
                query =>
                {
                    var q = (ListConversationMessages)query;
                    return $"/api/mailbox/conversation?id={Uri.EscapeDataString(q.ConversationId)}&subject={Uri.EscapeDataString(q.Subject ?? string.Empty)}";
                }));

        // Every attachment across that thread, grouped by the message that carried it — the
        // composer's "from this thread" source.
        queries.Register<ListConversationAttachments, IReadOnlyList<ConversationAttachmentGroup>>(
            new QueryRoute("/api/mailbox/conversation/attachments",
                query =>
                {
                    var q = (ListConversationAttachments)query;
                    return $"/api/mailbox/conversation/attachments?id={Uri.EscapeDataString(q.ConversationId)}&subject={Uri.EscapeDataString(q.Subject ?? string.Empty)}";
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
        // Superseded by SendMailboxEmail (mailbox/compose) — kept while older open tabs still call it.
        commands.Register<ReplyInThreadFromMessage, ReplyInThreadOutcome>(
            new CommandRoute("POST", "/api/mailbox/message/reply-in-thread", _ => "/api/mailbox/message/reply-in-thread"));

        // Triage compose: send (or stage) an email from the projects mailbox — replies and new
        // outbound emails alike. JSON path only here; sends with uploaded files go multipart,
        // posted directly by HttpIntakeQueue (same pattern as the progress-photo upload).
        commands.Register<Jewel.JPMS.Contracts.MailboxCompose.SendMailboxEmail, Jewel.JPMS.Contracts.MailboxCompose.ComposeOutcome>(
            new CommandRoute("POST", "/api/mailbox/compose", _ => "/api/mailbox/compose"));

        // Attachments: drawing revisions linked from the project register, and site photos. The
        // photo UPLOAD is multipart and posted directly by HttpRequestAttachmentStore, so — like
        // drawing revisions — it is deliberately not registered here.
        queries.Register<ListRequestAttachments, IReadOnlyList<RequestAttachment>>(
            new QueryRoute("/api/requests/{requestId}/attachments",
                query => $"/api/requests/{((ListRequestAttachments)query).RequestId}/attachments"));

        commands.Register<AttachDrawingsToRequest, IReadOnlyList<RequestAttachment>>(
            new CommandRoute("POST", "/api/requests/{requestId}/attachments/drawings",
                command => $"/api/requests/{((AttachDrawingsToRequest)command).RequestId}/attachments/drawings"));

        commands.Register<RemoveRequestAttachment, IReadOnlyList<RequestAttachment>>(
            new CommandRoute("DELETE", "/api/requests/{requestId}/attachments/{attachmentId}",
                command =>
                {
                    var remove = (RemoveRequestAttachment)command;
                    return $"/api/requests/{remove.RequestId}/attachments/{remove.RequestAttachmentId}";
                }));
    }
}
