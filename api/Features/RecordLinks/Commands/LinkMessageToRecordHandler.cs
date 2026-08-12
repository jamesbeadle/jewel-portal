using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

// Record-agnostic link: tag the email "JPMS/<record.TagReference>" (verified by read-back). The tag
// IS the association — no copy of the email is stored; the record reads its emails back live by the
// same tag. This is the single code path for linking an email to any record type; the legacy
// AssignMessageToRequest handler is a thin Request-typed adapter over this.
//
// This is also where the thread's communication PATHWAY is decided and guarded (the pathway split —
// docs/Pathway-Split-Platform-Flow-Plan.md §2.3). The record type implies a pathway (BucketFor);
// pathway-neutral types (CostCentre) take the triager's explicit choice from command.Pathway. Two
// tiers of protection:
//   • THE CLIENT WALL (hard): a thread can never carry Client and a non-Client pathway together.
//     Any link that would cross it is rejected — no override exists.
//   • THE LANES (soft): Subcontractor↔Internal dual filing is rejected by default but allowed with
//     an explicit AllowCrossPathway (the UI warns first).
public sealed class LinkMessageToRecordHandler : ICommandHandler<LinkMessageToRecord, Acknowledgement>
{
    private readonly RecordProviderRegistry providers;
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    private readonly AuditTrail audit;

    public LinkMessageToRecordHandler(
        RecordProviderRegistry providers, IMailboxGraphClient graph, RecordThreadTagger threadTagger, AuditTrail audit)
    {
        this.providers = providers;
        this.graph = graph;
        this.threadTagger = threadTagger;
        this.audit = audit;
    }

    public async Task<Acknowledgement> HandleAsync(LinkMessageToRecord command, CancellationToken cancellationToken)
    {
        var provider = providers.For(command.Type);

        var record = await provider.FindAsync(command.RecordId, cancellationToken)
            ?? throw new InvalidOperationException($"{command.Type} record '{command.RecordId}' not found.");

        // Closed requests stay linkable on purpose: a late reply on a closed RFI's thread files to
        // the record it belongs to without reopening it — the pickers show closed records for
        // exactly this (see RequestLinkProvider). The guard that refused them here (2026-07-16)
        // predated that decision and rejected the triage page's own picks; removed 2026-08-12.

        // Read the email back from the mailbox to confirm it's there and pick up a fresh threading id +
        // its conversation id (so we can tag the whole thread, not just this message) + its current
        // categories (so the pathway guards can see which bucket the thread already carries).
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        // The pathway this link would file the thread under: implied by the record type, or — for
        // pathway-neutral types like CostCentre — the triager's explicit choice. Null = no pathway
        // involvement at all (e.g. a Todo link, which never sets or changes one).
        var bucket = TriageCategories.BucketFor(record.Type) ?? MapPathway(command.Pathway);
        var existingBuckets = (snapshot.Categories ?? Array.Empty<string>())
            .Where(TriageCategories.IsBucketTag)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var hadBucket = existingBuckets.Count > 0;

        if (bucket is not null)
            foreach (var existing in existingBuckets.Where(e => !e.Equals(bucket, StringComparison.OrdinalIgnoreCase)))
            {
                if (TriageCategories.CrossesClientWall(existing, bucket))
                {
                    // The one combination that is never allowed, with no override: nothing filed on
                    // the client side may share a thread with subcontractor/internal correspondence.
                    await audit.WriteAsync(
                        AuditEventType.WallRejected,
                        $"Refused: linking {record.Reference} would file this thread under {AuditTrail.PathwayLabel(bucket)} but it is filed under {AuditTrail.PathwayLabel(existing)}.",
                        pathway: AuditTrail.PathwayLabel(existing),
                        projectId: NullIfEmpty(record.ProjectId),
                        recordType: record.Type,
                        recordId: record.RecordId,
                        recordReference: record.Reference,
                        conversationId: snapshot.ConversationId,
                        emailMessageId: command.MessageId,
                        internetMessageId: snapshot.InternetMessageId,
                        cancellationToken: cancellationToken);
                    throw new InvalidOperationException(
                        $"This thread is filed under {AuditTrail.PathwayLabel(existing)}; {record.Reference} would file it under {AuditTrail.PathwayLabel(bucket)}. "
                        + "Client correspondence is never mixed with subcontractor or internal correspondence — start a new thread, or forward the relevant content.");
                }

                if (!command.AllowCrossPathway)
                    throw new InvalidOperationException(
                        $"This thread is filed under {AuditTrail.PathwayLabel(existing)}; {record.Reference} would also file it under {AuditTrail.PathwayLabel(bucket)}. "
                        + "Confirm the cross-filing to proceed, or link the email to a record on the same pathway.");
            }

        // The tag is the only link. How far it spreads is the command's Scope (2026-08-07): the
        // default sweeps the anchor plus the thread behind it so the record sees the full context;
        // MessageOnly suppresses the conversation sweep entirely (the anchor alone is tagged, its
        // siblings keep queueing); EntireThread drops the received-at cutoff so the whole current
        // conversation — newer replies included — is tagged. The anchor tag is verified; sibling
        // (reply/forward) tagging is best-effort.
        var sweepConversationId = command.Scope == LinkThreadScope.MessageOnly ? null : snapshot.ConversationId;
        DateTimeOffset? sweepCutoff = command.Scope == LinkThreadScope.EntireThread ? null : snapshot.ReceivedAt;
        var tagged = await threadTagger.TagThreadAsync(
            command.MessageId, snapshot.InternetMessageId, sweepConversationId,
            TriageCategories.ForRecord(record.TagReference), cancellationToken,
            anchorReceivedAt: sweepCutoff);
        if (!tagged)
            throw new InvalidOperationException("The email couldn't be tagged to the record. Please try again.");

        // File the pathway alongside the record tag, with the same spread as above (best-effort: the
        // record tag is the primary association; a missed bucket stamp is healed by the backfill /
        // conflict report). MessageOnly must scope this too — a thread-wide bucket stamp carries the
        // JPMS marker, which would pull the untagged siblings out of the queue.
        var stampedBucket = false;
        if (bucket is not null && !existingBuckets.Contains(bucket, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                stampedBucket = await threadTagger.TagThreadAsync(
                    command.MessageId, snapshot.InternetMessageId, sweepConversationId, bucket, cancellationToken,
                    anchorReceivedAt: sweepCutoff);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { /* best-effort */ }
        }

        // Audit — every successful link is recorded, whatever the pathway (scope widened
        // 2026-08-04 along the route the event model reserved: see AuditEventType). The record
        // activity indicator derives each record's recent-communications score from these rows,
        // so a bid package or work order link matters as much as a client one. Pathway carries
        // the thread's side ("" for pathway-neutral links like a Todo), and the client-facing
        // views keep filtering on it — exactly as the 2026-07-22 scope decision anticipated.
        var resultingBucket = bucket ?? existingBuckets.FirstOrDefault();
        var pathwayLabel = AuditTrail.PathwayLabel(resultingBucket);
        await audit.WriteAsync(
            !hadBucket && stampedBucket ? AuditEventType.EmailTriaged : AuditEventType.RecordLinked,
            !hadBucket && stampedBucket
                ? $"Email filed under {pathwayLabel} and linked to {record.Reference}."
                : $"Email linked to {record.Reference}.",
            pathway: pathwayLabel,
            projectId: NullIfEmpty(record.ProjectId),
            recordType: record.Type,
            recordId: record.RecordId,
            recordReference: record.Reference,
            conversationId: snapshot.ConversationId,
            emailMessageId: command.MessageId,
            internetMessageId: snapshot.InternetMessageId,
            cancellationToken: cancellationToken);

        return new Acknowledgement(record.RecordId);
    }

    // The triager's explicit pathway choice ("Client" / "Subcontractor" / "Internal") as its bucket
    // category. Unknown/blank → null (no pathway involvement).
    private static string? MapPathway(string? pathway)
    {
        if (string.IsNullOrWhiteSpace(pathway)) return null;
        var p = pathway.Trim();
        if (p.Equals("Client", StringComparison.OrdinalIgnoreCase)) return TriageCategories.Client;
        if (p.Equals("Subcontractor", StringComparison.OrdinalIgnoreCase)) return TriageCategories.Subcontractor;
        if (p.Equals("Internal", StringComparison.OrdinalIgnoreCase)) return TriageCategories.Internal;
        return null;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
