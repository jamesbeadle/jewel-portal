using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Assign a mailbox message to an existing request (live-read model). Records the email on the
/// request's shared conversation and moves the message out of the Inbox into the request's folder.
/// </summary>
public sealed class AssignMessageToRequestHandler : ICommandHandler<AssignMessageToRequest, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    public AssignMessageToRequestHandler(JpmsContext context, IMailboxGraphClient graph) { this.context = context; this.graph = graph; }

    public async Task<Acknowledgement> HandleAsync(AssignMessageToRequest command, CancellationToken cancellationToken)
    {
        var request = await context.Requests.FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request {command.RequestId} not found.");

        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        context.RequestMessages.Add(MailboxRequestMessage.From(snapshot, request.RequestId));
        await context.SaveChangesAsync(cancellationToken);

        // Move the email into the request's folder; remember the folder on first use.
        var folderId = await graph.MoveToRequestFolderAsync(
            command.MessageId, snapshot.InternetMessageId, RequestsIdentifierFactory.FolderName(request.Number), cancellationToken);
        if (!string.IsNullOrEmpty(folderId) && string.IsNullOrEmpty(request.MailboxFolderId))
        {
            request.MailboxFolderId = folderId;
            await context.SaveChangesAsync(cancellationToken);
        }

        return new Acknowledgement(request.RequestId);
    }
}

/// <summary>
/// Create a brand-new request from a mailbox message (live-read model). Creates the request, records
/// the email as the opening conversation message, moves the email into the new request's folder, and
/// queues the request document to the project's contacts (the worker still owns outbound sending).
/// </summary>
public sealed class CreateRequestFromMessageHandler : ICommandHandler<CreateRequestFromMessage, Request>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly IMailboxActionScheduler mailbox;
    public CreateRequestFromMessageHandler(JpmsContext context, IMailboxGraphClient graph, IMailboxActionScheduler mailbox)
    { this.context = context; this.graph = graph; this.mailbox = mailbox; }

    public async Task<Request> HandleAsync(CreateRequestFromMessage command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        var nextNumber = (await context.Requests.MaxAsync(r => (int?)r.Number, cancellationToken) ?? 0) + 1;

        var request = new RequestEntity
        {
            RequestId = RequestsIdentifierFactory.Next(),
            Number = nextNumber,
            ProjectId = command.ProjectId,
            Kind = (int)command.Kind,
            Reference = command.Reference,
            Title = Clamp(command.Title, 256),
            Description = Clamp(command.Description, 2048),
            Status = (int)RequestStatus.Open,
            Value = command.Value,
            RaisedByEmail = command.RaisedByEmail,
            RaisedAt = snapshot.ReceivedAt,
            ImpliesVariation = false,
            RaisedTo = command.RaisedTo,
            DrawingRef = command.DrawingRef,
            ResponseDue = command.ResponseDue
        };
        context.Requests.Add(request);
        context.RequestMessages.Add(MailboxRequestMessage.From(snapshot, request.RequestId));

        var folderId = await graph.MoveToRequestFolderAsync(
            command.MessageId, snapshot.InternetMessageId, RequestsIdentifierFactory.FolderName(request.Number), cancellationToken);
        if (!string.IsNullOrEmpty(folderId))
            request.MailboxFolderId = folderId;

        await context.SaveChangesAsync(cancellationToken);

        // Issue the rendered document to the project's contacts (no-op when unconfigured / no contacts).
        await mailbox.ScheduleRequestDocumentSendAsync(request.RequestId, recipientOverride: null, cancellationToken);

        return request.ToModel();
    }

    // Email subjects/bodies can exceed the request column limits; clamp so a long email can't throw on save.
    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}

/// <summary>Builds the inbound, shared conversation entry that records a mailbox message against a
/// request — the live-read equivalent of IntakeConversation, sourced from a live snapshot.</summary>
internal static class MailboxRequestMessage
{
    public static RequestMessageEntity From(MailboxSnapshot s, string requestId) => new()
    {
        MessageId = RequestsIdentifierFactory.Next(),
        RequestId = requestId,
        AuthorEmail = s.FromEmail,
        AuthorName = string.IsNullOrWhiteSpace(s.FromName) ? s.FromEmail : s.FromName,
        Body = string.IsNullOrWhiteSpace(s.BodyPreview) ? "(no message body)" : s.BodyPreview,
        Visibility = (int)MessageVisibility.Shared,
        PostedAt = s.ReceivedAt,
        Direction = (int)MessageDirection.Inbound,
        EmailMessageId = s.InternetMessageId,
        InReplyTo = s.InReplyTo,
        ConversationId = s.ConversationId,
        SentStatus = (int)MessageSentStatus.NotApplicable
    };
}
