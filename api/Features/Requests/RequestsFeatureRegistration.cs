using Jewel.JPMS.Api.Features.Requests.Attachments;
using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Api.Features.Requests.Queries;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Requests;

public static class RequestsFeatureRegistration
{
    public static IServiceCollection AddRequestsFeature(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterAttachmentStore(services, configuration);

        // Attachments on a request: linked drawing revisions and uploaded site photos.
        services.AddScoped<IQueryHandler<ListRequestAttachments, IReadOnlyList<RequestAttachment>>,
            ListRequestAttachmentsHandler>();
        services.AddScoped<ICommandHandler<AttachDrawingsToRequest, IReadOnlyList<RequestAttachment>>,
            AttachDrawingsToRequestHandler>();
        services.AddScoped<ICommandHandler<RemoveRequestAttachment, IReadOnlyList<RequestAttachment>>,
            RemoveRequestAttachmentHandler>();

        services.AddScoped<IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>>, ListRequestsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetRequestById, Request?>, GetRequestByIdHandler>();
        services.AddScoped<IQueryHandler<GetRequestDocument, RequestDocumentFile?>, GetRequestDocumentHandler>();
        services.AddScoped<IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>>, ListRequestMessagesHandler>();
        services.AddScoped<IQueryHandler<GetRequestEmailDetail, MailboxMessageDetail>, GetRequestEmailDetailHandler>();
        services.AddScoped<IQueryHandler<ListUnassignedRequests, IReadOnlyList<Request>>, ListUnassignedRequestsHandler>();
        // Cross-project RFI dashboard: every RFI on every live project in one register.
        services.AddScoped<IQueryHandler<ListRfisAcrossProjects, IReadOnlyList<Request>>, ListRfisAcrossProjectsHandler>();

        // Recipients preview: the exact To/CC/BCC set an issue or draft would use right now,
        // resolved through the same shared RequestRecipientResolver as the send paths.
        services.AddScoped<IQueryHandler<ResolveRequestRecipients, RequestRecipientSet>, ResolveRequestRecipientsHandler>();

        // Reads a request's emails live from the mailbox by its workflow tag — the replacement for the
        // old stored email snapshot. Used by the conversation view, LLM context, and document builder.
        services.AddScoped<RequestEmailReader>();
        services.AddScoped<RequestContextAssembler>();

        services.AddScoped<ICommandHandler<CloseRequest, RequestCloseOutcome>, CloseRequestHandler>();
        services.AddScoped<CloseRequestAuthorisation>();
        services.AddScoped<CloseRequestValidation>();

        // Live-read triage: read the Inbox (queue) / General (discarded) folder straight from the
        // mailbox, move messages (discard/restore), and assign/create requests from a message.
        services.AddScoped<IQueryHandler<ListInboxMessages, MailboxPage>, ListInboxMessagesHandler>();
        services.AddScoped<AutoReplySweeper>();
        services.AddScoped<IQueryHandler<ListDiscardedMessages, MailboxPage>, ListDiscardedMessagesHandler>();
        services.AddScoped<IQueryHandler<ListTaggedMessages, MailboxPage>, ListTaggedMessagesHandler>();
        services.AddScoped<IQueryHandler<ListConversationMessages, MailboxPage>, ListConversationMessagesHandler>();
        services.AddScoped<IQueryHandler<ListConversationAttachments, IReadOnlyList<ConversationAttachmentGroup>>, ListConversationAttachmentsHandler>();
        services.AddScoped<IQueryHandler<GetMailboxMessageDetail, MailboxMessageDetail>, GetMailboxMessageDetailHandler>();
        services.AddScoped<ICommandHandler<DiscardMessage, Acknowledgement>, DiscardMessageHandler>();
        services.AddScoped<ICommandHandler<RestoreMessage, Acknowledgement>, RestoreMessageHandler>();
        services.AddScoped<ICommandHandler<RemoveTagFromMessage, Acknowledgement>, RemoveTagFromMessageHandler>();
        services.AddScoped<ICommandHandler<AssignMessageToRequest, Acknowledgement>, AssignMessageToRequestHandler>();
        services.AddScoped<ICommandHandler<CreateRequestFromMessage, Request>, CreateRequestFromMessageHandler>();
        // The gate classes the connector's action gateway composes (2026-08-31).
        services.AddScoped<DiscardMessageAuthorisation>();
        services.AddScoped<RestoreMessageAuthorisation>();
        services.AddScoped<RemoveTagFromMessageAuthorisation>();
        services.AddScoped<CreateRequestFromMessageAuthorisation>();
        // Triage "Reply in thread": reply draft in the projects mailbox + a background General
        // request from the same email, in one action.
        services.AddScoped<ICommandHandler<ReplyInThreadFromMessage, ReplyInThreadOutcome>, ReplyInThreadFromMessageHandler>();

        // One-off admin sweep migrating legacy flat request tags to project-qualified ones.
        services.AddScoped<ICommandHandler<RetagRequestWorkflowTags, RequestRetagSummary>, RetagRequestWorkflowTagsHandler>();

        services.AddScoped<ICommandHandler<RaiseRequest, Request>, RaiseRequestHandler>();
        services.AddScoped<RaiseRequestAuthorisation>();
        services.AddScoped<RaiseRequestValidation>();

        services.AddScoped<ICommandHandler<UpdateRequestDetails, Request>, UpdateRequestDetailsHandler>();
        services.AddScoped<UpdateRequestDetailsAuthorisation>();
        services.AddScoped<UpdateRequestDetailsValidation>();

        services.AddScoped<ICommandHandler<UpdateRequestForm, Request>, UpdateRequestFormHandler>();
        services.AddScoped<UpdateRequestFormAuthorisation>();
        services.AddScoped<UpdateRequestFormValidation>();

        services.AddScoped<ICommandHandler<PrepareRequestEmailDraft, RequestEmailDraft>, PrepareRequestEmailDraftHandler>();
        services.AddScoped<PrepareRequestEmailDraftAuthorisation>();
        services.AddScoped<PrepareRequestEmailDraftValidation>();

        // Reply drafting: the official PDF staged as a reply inside an existing email thread.
        services.AddScoped<ICommandHandler<PrepareRequestReplyDraft, RequestEmailDraft>, PrepareRequestReplyDraftHandler>();
        services.AddScoped<PrepareRequestReplyDraftAuthorisation>();
        services.AddScoped<PrepareRequestReplyDraftValidation>();

        // Bulk drafting: one Outlook draft per selected request, delegating each to the single
        // handler above so the drafts are identical to detail-page ones.
        services.AddScoped<ICommandHandler<PrepareRequestEmailDrafts, RequestEmailDraftBatch>, PrepareRequestEmailDraftsHandler>();
        services.AddScoped<PrepareRequestEmailDraftsAuthorisation>();
        services.AddScoped<PrepareRequestEmailDraftsValidation>();

        services.AddScoped<ICommandHandler<PromoteRequestToRfi, Request>, PromoteRequestToRfiHandler>();
        services.AddScoped<PromoteRequestToRfiAuthorisation>();
        services.AddScoped<PromoteRequestToRfiValidation>();

        services.AddScoped<ICommandHandler<EnableRfqOnRequest, Request>, EnableRfqOnRequestHandler>();
        services.AddScoped<EnableRfqOnRequestAuthorisation>();
        services.AddScoped<EnableRfqOnRequestValidation>();

        services.AddScoped<ICommandHandler<LinkRequestToParty, Request>, LinkRequestToPartyHandler>();
        services.AddScoped<LinkRequestToPartyAuthorisation>();
        services.AddScoped<LinkRequestToPartyValidation>();

        services.AddScoped<ICommandHandler<PostRequestMessage, RequestMessage>, PostRequestMessageHandler>();
        services.AddScoped<PostRequestMessageAuthorisation>();
        services.AddScoped<PostRequestMessageValidation>();

        // Pre-RFI merge: fold one General request into another (survivor keeps its identity;
        // conversation, items and emails follow; the merged request closes with an audit link).
        services.AddScoped<ICommandHandler<MergeRequests, Request>, MergeRequestsHandler>();
        services.AddScoped<MergeRequestsAuthorisation>();
        services.AddScoped<MergeRequestsValidation>();

        services.AddScoped<ICommandHandler<DeleteRequest, Acknowledgement>, DeleteRequestHandler>();
        services.AddScoped<DeleteRequestAuthorisation>();
        services.AddScoped<DeleteRequestValidation>();

        services.AddScoped<ICommandHandler<ReturnRequestToTriage, Acknowledgement>, ReturnRequestToTriageHandler>();
        services.AddScoped<ReturnRequestToTriageAuthorisation>();
        services.AddScoped<ReturnRequestToTriageValidation>();

        services.AddScoped<ICommandHandler<ResendRequestDocument, Acknowledgement>, ResendRequestDocumentHandler>();
        services.AddScoped<ResendRequestDocumentAuthorisation>();
        services.AddScoped<ResendRequestDocumentValidation>();

        // Draft withdrawal: delete ONE unsent draft from the shared mailbox's Drafts folder (the
        // Graph client verifies it really is an unsent draft before the DELETE fires).
        services.AddScoped<ICommandHandler<DeleteMailboxDraft, Acknowledgement>, DeleteMailboxDraftHandler>();
        services.AddScoped<DeleteMailboxDraftAuthorisation>();
        services.AddScoped<DeleteMailboxDraftValidation>();

        return services;
    }

    // Request attachments share the drawings storage account by default — one connection string to
    // configure, one backup story — but can be pointed at their own account if photo volume ever
    // warrants it.
    private static void RegisterAttachmentStore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["RequestAttachmentsStorage:ConnectionString"]
            ?? configuration["DrawingsStorage:ConnectionString"]
            ?? configuration["AzureWebJobsStorage"];

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddSingleton<IRequestAttachmentStore, NullRequestAttachmentStore>();
        else
            services.AddSingleton<IRequestAttachmentStore>(
                _ => new AzureBlobRequestAttachmentStore(connectionString));
    }
}
