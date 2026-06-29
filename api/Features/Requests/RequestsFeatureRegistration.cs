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
        services.AddScoped<IQueryHandler<ListUnassignedRequests, IReadOnlyList<Request>>, ListUnassignedRequestsHandler>();

        // Live-read triage: read the Inbox (queue) / General (discarded) folder straight from the
        // mailbox, move messages (discard/restore), and assign/create requests from a message.
        services.AddScoped<IQueryHandler<ListInboxMessages, PagedResult<MailboxMessage>>, ListInboxMessagesHandler>();
        services.AddScoped<IQueryHandler<ListDiscardedMessages, PagedResult<MailboxMessage>>, ListDiscardedMessagesHandler>();
        services.AddScoped<IQueryHandler<GetMailboxMessageDetail, MailboxMessageDetail>, GetMailboxMessageDetailHandler>();
        services.AddScoped<ICommandHandler<DiscardMessage, Acknowledgement>, DiscardMessageHandler>();
        services.AddScoped<ICommandHandler<RestoreMessage, Acknowledgement>, RestoreMessageHandler>();
        services.AddScoped<ICommandHandler<AssignMessageToRequest, Acknowledgement>, AssignMessageToRequestHandler>();
        services.AddScoped<ICommandHandler<CreateRequestFromMessage, Request>, CreateRequestFromMessageHandler>();

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

        services.AddScoped<ICommandHandler<ReturnRequestToTriage, Acknowledgement>, ReturnRequestToTriageHandler>();
        services.AddScoped<ReturnRequestToTriageAuthorisation>();
        services.AddScoped<ReturnRequestToTriageValidation>();

        services.AddScoped<ICommandHandler<ResendRequestDocument, Acknowledgement>, ResendRequestDocumentHandler>();
        services.AddScoped<ResendRequestDocumentAuthorisation>();
        services.AddScoped<ResendRequestDocumentValidation>();

        return services;
    }
}
