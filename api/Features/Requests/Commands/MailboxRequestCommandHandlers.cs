using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Assign a mailbox message to an existing request. This is now a thin Request-typed adapter over the
/// record-agnostic <see cref="LinkMessageToRecord"/> path: it forwards to the generic handler with
/// <see cref="RecordType.Request"/>. Behaviour is identical (tag "JPMS/&lt;ref&gt;", verified, no copy);
/// the adapter is kept so existing callers (the triage "link to existing" / "add tag" flows) keep
/// working while the UI migrates to the generic command.
/// </summary>
public sealed class AssignMessageToRequestHandler : ICommandHandler<AssignMessageToRequest, Acknowledgement>
{
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;
    public AssignMessageToRequestHandler(ICommandHandler<LinkMessageToRecord, Acknowledgement> link) { this.link = link; }

    public Task<Acknowledgement> HandleAsync(AssignMessageToRequest command, CancellationToken cancellationToken) =>
        link.HandleAsync(
            new LinkMessageToRecord(command.MessageId, RecordType.Request, command.RequestId, command.InternetMessageId),
            cancellationToken);
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

        await RequestReferenceGuard.EnsureUniqueAsync(context, command.ProjectId, command.Reference, excludeRequestId: null, cancellationToken);

        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        var nextNumber = (await context.Requests.MaxAsync(r => (int?)r.Number, cancellationToken) ?? 0) + 1;

        // A blank reference is minted server-side by kind: a General container is auto-numbered
        // REQ-#### to match its display number (global sequence); any other kind continues the
        // project's own sequence (e.g. "RFI-048" -> "RFI-049"). A specific typed reference is
        // honoured as-is.
        string reference;
        if (!string.IsNullOrWhiteSpace(command.Reference))
        {
            reference = command.Reference.Trim();
        }
        else if (command.Kind == RequestType.General)
        {
            reference = $"REQ-{nextNumber:0000}";
        }
        else
        {
            var projectReferences = await context.Requests
                .Where(r => r.ProjectId == command.ProjectId)
                .Select(r => r.Reference)
                .ToListAsync(cancellationToken);
            reference = RequestReference.SuggestNext(command.Kind, projectReferences);
        }

        var request = new RequestEntity
        {
            RequestId = RequestsIdentifierFactory.Next(),
            Number = nextNumber,
            ProjectId = command.ProjectId,
            Kind = (int)command.Kind,
            Reference = reference,
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
        // Tag the email to this new request first, verified by read-back; only persist the request
        // once the tag sticks, so we never create a request whose email is still sitting in the queue.
        var tag = TriageCategories.ForRequest(
            RequestTags.Stem(await RequestTags.ProjectRefAsync(context, command.ProjectId, cancellationToken), command.ProjectId, request.TagReference));
        var tagged = await graph.AssignAsync(command.MessageId, snapshot.InternetMessageId, tag, cancellationToken);
        if (!tagged)
            throw new InvalidOperationException("The email couldn't be tagged to the new request. Please try again.");

        // The tag is the only link to the email — no copy is stored. The request reads its emails live
        // by tag (RequestEmailReader) for the conversation view, LLM context, and document.
        context.Requests.Add(request);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (RequestReferenceConflict.IsReferenceClash(ex))
        {
            // Lost a race for that reference after the email was already tagged: pull the tag back off
            // (best-effort) so the email stays in the triage queue, then surface the clash.
            try { await graph.ClearRequestTagsAsync(tag, cancellationToken); } catch { /* best-effort */ }
            throw RequestReferenceConflict.AsFriendlyError(reference);
        }

        // Issue the rendered document to the project's contacts (no-op when unconfigured / no contacts).
        await mailbox.ScheduleRequestDocumentSendAsync(request.RequestId, recipientOverride: null, cancellationToken);

        return request.ToModel();
    }

    // Email subjects/bodies can exceed the request column limits; clamp so a long email can't throw on save.
    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
