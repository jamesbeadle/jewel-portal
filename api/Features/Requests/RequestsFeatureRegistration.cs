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
        services.AddScoped<IQueryHandler<GetRequestDocument, RequestDocumentFile?>, GetRequestDocumentHandler>();
        services.AddScoped<IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>>, ListRequestMessagesHandler>();
        services.AddScoped<IQueryHandler<GetRequestEmailDetail, MailboxMessageDetail>, GetRequestEmailDetailHandler>();
        services.AddScoped<IQueryHandler<ListUnassignedRequests, IReadOnlyList<Request>>, ListUnassignedRequestsHandler>();

        // Recipients preview: the exact To/CC/BCC set an issue or draft would use right now,
        // resolved through the same shared RequestRecipientResolver as the send paths.
        services.AddScoped<IQueryHandler<ResolveRequestRecipients, RequestRecipientSet>, ResolveRequestRecipientsHandler>();

        // Reads a request's emails live from the mailbox by its workflow tag — the replacement for the
        // old stored email snapshot. Used by the conversation view, LLM context, and document builder.
        services.AddScoped<RequestEmailReader>();

        // Live-read triage: read the Inbox (queue) / General (discarded) folder straight from the
        // mailbox, move messages (discard/restore), and assign/create requests from a message.
        services.AddScoped<IQueryHandler<ListInboxMessages, MailboxPage>, ListInboxMessagesHandler>();
        services.AddScoped<IQueryHandler<ListDiscardedMessages, MailboxPage>, ListDiscardedMessagesHandler>();
        services.AddScoped<IQueryHandler<ListTaggedMessages, MailboxPage>, ListTaggedMessagesHandler>();
        services.AddScoped<IQueryHandler<GetMailboxMessageDetail, MailboxMessageDetail>, GetMailboxMessageDetailHandler>();
        services.AddScoped<ICommandHandler<DiscardMessage, Acknowledgement>, DiscardMessageHandler>();
        services.AddScoped<ICommandHandler<RestoreMessage, Acknowledgement>, RestoreMessageHandler>();
        services.AddScoped<ICommandHandler<RemoveTagFromMessage, Acknowledgement>, RemoveTagFromMessageHandler>();
        services.AddScoped<ICommandHandler<AssignMessageToRequest, Acknowledgement>, AssignMessageToRequestHandler>();
        services.AddScoped<ICommandHandler<CreateRequestFromMessage, Request>, CreateRequestFromMessageHandler>();

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

        services.AddScoped<ICommandHandler<DeleteRequest, Acknowledgement>, DeleteRequestHandler>();
        services.AddScoped<DeleteRequestAuthorisation>();
        services.AddScoped<DeleteRequestValidation>();

        services.AddScoped<ICommandHandler<ReturnRequestToTriage, Acknowledgement>, ReturnRequestToTriageHandler>();
        services.AddScoped<ReturnRequestToTriageAuthorisation>();
        services.AddScoped<ReturnRequestToTriageValidation>();

        services.AddScoped<ICommandHandler<ResendRequestDocument, Acknowledgement>, ResendRequestDocumentHandler>();
        services.AddScoped<ResendRequestDocumentAuthorisation>();
        services.AddScoped<ResendRequestDocumentValidation>();

        return services;
    }
}
