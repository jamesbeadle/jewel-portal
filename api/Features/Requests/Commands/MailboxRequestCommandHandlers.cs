using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
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
/// Create a brand-new request from a mailbox message (live-read model). Creates the request and tags
/// the email to it — and, like the link-to-existing path, the tag is applied across the email's whole
/// conversation (anchor verified, siblings best-effort via <see cref="RecordThreadTagger"/>) so the
/// entire thread leaves the triage queue together, not just the one clicked message. This is also the
/// path "Reply in thread" delegates to, so replying triages the whole thread as well. No document
/// email is drafted here — drafts are only created when explicitly requested
/// (PrepareRequestEmailDraft / PrepareRequestReplyDraft).
/// </summary>
public sealed class CreateRequestFromMessageHandler : ICommandHandler<CreateRequestFromMessage, Request>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> linkToRecord;
    private readonly AuditTrail audit;
    public CreateRequestFromMessageHandler(
        JpmsContext context, IMailboxGraphClient graph, RecordThreadTagger threadTagger,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> linkToRecord, AuditTrail audit)
    { this.context = context; this.graph = graph; this.threadTagger = threadTagger; this.linkToRecord = linkToRecord; this.audit = audit; }

    public async Task<Request> HandleAsync(CreateRequestFromMessage command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        await RequestReferenceGuard.EnsureUniqueAsync(context, command.ProjectId, command.Reference, excludeRequestId: null, cancellationToken);

        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        // THE CLIENT WALL (docs/Pathway-Split-Platform-Flow-Plan.md §2.3): a request files its
        // thread under Client, so a thread already filed under Subcontractor or Internal can never
        // become a request — refused before anything is tagged or created, with no override.
        var existingBucket = (snapshot.Categories ?? Array.Empty<string>())
            .FirstOrDefault(TriageCategories.IsBucketTag);
        if (existingBucket is not null
            && !existingBucket.Equals(TriageCategories.Client, StringComparison.OrdinalIgnoreCase))
        {
            await audit.WriteAsync(
                AuditEventType.WallRejected,
                $"Refused: creating a request would file this thread under Client but it is filed under {AuditTrail.PathwayLabel(existingBucket)}.",
                pathway: AuditTrail.PathwayLabel(existingBucket),
                projectId: command.ProjectId,
                recordType: RecordType.Request,
                conversationId: snapshot.ConversationId,
                emailMessageId: command.MessageId,
                internetMessageId: snapshot.InternetMessageId,
                cancellationToken: cancellationToken);
            throw new InvalidOperationException(
                $"This thread is filed under {AuditTrail.PathwayLabel(existingBucket)}; a request would file it under Client. "
                + "Client correspondence is never mixed with subcontractor or internal correspondence — start a new thread, or forward the relevant content.");
        }

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
            // The one visible date: a request born from an email takes the email's received
            // date as its issue date (user-editable thereafter).
            IssuedAt = snapshot.ReceivedAt,
            ImpliesVariation = false,
            RaisedTo = command.RaisedTo,
            DrawingRef = command.DrawingRef,
            ResponseDue = command.ResponseDue
        };
        // Tag the email to this new request first, verified by read-back; only persist the request
        // once the tag sticks, so we never create a request whose email is still sitting in the queue.
        // The tag spans the whole conversation (siblings best-effort), matching the link-to-existing
        // path — otherwise older messages and replies in the same thread would stay in triage.
        var tag = TriageCategories.ForRequest(
            RequestTags.Stem(await RequestTags.ProjectRefAsync(context, command.ProjectId, cancellationToken), command.ProjectId, request.TagReference));
        var tagged = await threadTagger.TagThreadAsync(
            command.MessageId, snapshot.InternetMessageId, snapshot.ConversationId, tag, cancellationToken);
        if (!tagged)
            throw new InvalidOperationException("The email couldn't be tagged to the new request. Please try again.");

        // The tag is the only link to the email — no copy is stored. The request reads its emails live
        // by tag (RequestEmailReader) for the conversation view and LLM context (never the issued
        // document, which carries no correspondence).
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

        // File the thread under the Client pathway (thread-wide, best-effort — the record tag is the
        // primary association; a missed stamp is healed by the backfill). Stamped after the save so
        // a reference-clash rollback can never leave a pathway-only thread behind.
        var stampedClient = false;
        if (existingBucket is null)
        {
            try
            {
                stampedClient = await threadTagger.TagThreadAsync(
                    command.MessageId, snapshot.InternetMessageId, snapshot.ConversationId,
                    TriageCategories.Client, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { /* best-effort */ }
        }

        await audit.WriteAsync(
            stampedClient ? AuditEventType.EmailTriaged : AuditEventType.RecordCreatedFromEmail,
            stampedClient
                ? $"Email filed under Client; {reference} created from it."
                : $"{reference} created from email.",
            pathway: "Client",
            projectId: command.ProjectId,
            recordType: RecordType.Request,
            recordId: request.RequestId,
            recordReference: reference,
            conversationId: snapshot.ConversationId,
            emailMessageId: command.MessageId,
            internetMessageId: snapshot.InternetMessageId,
            cancellationToken: cancellationToken);

        // "Also add to Programme": tag the same thread to the project's Scheduling bucket as well,
        // by delegating to the record-agnostic link path — the exact action the standalone
        // "Tag email to programme" button performs — so the "SCH-<projectRef>" stem rule stays in
        // one place (SchedulingLinkProvider). Applied only after the request is saved, so a
        // reference-clash rollback can never leave a stray programme tag behind. If this second tag
        // fails the request already exists and the email is linked to it, so say exactly that and
        // how to finish by hand instead of reporting a bare failure.
        if (command.AddToProgramme)
        {
            try
            {
                await linkToRecord.HandleAsync(
                    new LinkMessageToRecord(command.MessageId, RecordType.Scheduling, command.ProjectId, snapshot.InternetMessageId),
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException(
                    $"{reference} was created and the email is linked to it, but the email couldn't also be tagged to the programme. Add the programme tag from the Tagged view.", ex);
            }
        }

        // No email is drafted here — a draft is only created when a person explicitly asks for one
        // (PrepareRequestEmailDraft / PrepareRequestReplyDraft).
        return request.ToModel();
    }

    // Email subjects/bodies can exceed the request column limits; clamp so a long email can't throw on save.
    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
