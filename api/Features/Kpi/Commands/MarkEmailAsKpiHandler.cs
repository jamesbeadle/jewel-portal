using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Kpi;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.Kpi.Commands;

// Marks a mailbox email as a KPI against a person. The email is read back from the mailbox (so a
// stale queue row cannot mark something that has since gone) and its envelope snapshotted onto
// the row. The KPI itself never reaches the mailbox (the register is administrators-only), but
// the email IS tagged JPMS/Admin + the Internal pathway once the row is saved (2026-09-03): the
// triage queue is "Inbox without a JPMS tag", so without a tag a KPI email sat in the queue for
// ever. The tag says only that an administrator dealt with it — nothing about why. One mark per
// email per person: marking the same email for the same person twice answers with the existing
// row rather than a twin (a new note replaces the old) and re-applies the tag, which is how a
// mark whose tagging failed first time is healed.
public sealed class MarkEmailAsKpiHandler : ICommandHandler<MarkEmailAsKpi, KpiEmail>
{
    private readonly JpmsContext context;
    private readonly KpiPersonResolver people;
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    private readonly AuditTrail audit;

    public MarkEmailAsKpiHandler(
        JpmsContext context, KpiPersonResolver people, IMailboxGraphClient graph,
        RecordThreadTagger threadTagger, AuditTrail audit)
    {
        this.context = context; this.people = people; this.graph = graph;
        this.threadTagger = threadTagger; this.audit = audit;
    }

    public async Task<KpiEmail> HandleAsync(MarkEmailAsKpi command, CancellationToken cancellationToken)
    {
        var person = await people.ResolveAsync(command.PersonId, command.PersonEmail, command.PersonName, cancellationToken);

        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        var existing = await context.KpiEmails
            .FirstOrDefaultAsync(row => row.PersonId == person.KpiPersonId && row.InternetMessageId == snapshot.InternetMessageId, cancellationToken);
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(command.Note) && existing.Note != command.Note.Trim())
                existing.Note = command.Note.Trim();
            await context.SaveChangesAsync(cancellationToken); // also lands a freshly minted person
            await TagOutOfQueueAsync(command, snapshot, cancellationToken);
            return existing.ToModel(person);
        }

        // Highest existing number + 1, never a row count — removals leave gaps.
        var nextNumber = (await context.KpiEmails.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;

        var entity = new KpiEmailEntity
        {
            KpiEmailId = Guid.NewGuid().ToString("N"),
            PersonId = person.KpiPersonId,
            MessageId = command.MessageId,
            InternetMessageId = snapshot.InternetMessageId,
            ConversationId = snapshot.ConversationId,
            Subject = Clamp(snapshot.Subject, 1024),
            FromEmail = Clamp(snapshot.FromEmail, 256),
            FromName = Clamp(snapshot.FromName, 256),
            ReceivedAt = snapshot.ReceivedAt,
            Note = (command.Note ?? "").Trim(),
            MarkedByEmail = command.MarkedByEmail,
            MarkedAt = DateTimeOffset.UtcNow,
            Number = nextNumber
        };
        context.KpiEmails.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        // Reference only — the register is administrators-only, so the audit row (which wider
        // roles can read) names neither the person nor the email.
        await audit.WriteAsync(
            AuditEventType.KpiEmailMarked,
            $"{entity.Reference} marked",
            recordReference: entity.Reference,
            actorEmail: string.IsNullOrWhiteSpace(command.MarkedByEmail) ? null : command.MarkedByEmail,
            cancellationToken: cancellationToken);

        await TagOutOfQueueAsync(command, snapshot, cancellationToken);
        return entity.ToModel(person);
    }

    // Take the email out of the triage queue: JPMS/Admin on the anchor (verified by read-back)
    // and, when the thread carries no pathway yet, the Internal pathway alongside it (best-effort,
    // like LinkMessageToRecord's bucket stamp). Spread follows the command's Scope exactly as a
    // record link does — MessageOnly tags the anchor alone; EntireThread sweeps the whole current
    // conversation. Runs AFTER the row is saved, so a tagging failure leaves the mark in place and
    // the email visibly queued: the thrown message tells the administrator to Apply again, which
    // hits the existing-row path and simply re-tags.
    private async Task TagOutOfQueueAsync(MarkEmailAsKpi command, MailboxSnapshot snapshot, CancellationToken cancellationToken)
    {
        var sweepConversationId = command.Scope == LinkThreadScope.MessageOnly ? null : snapshot.ConversationId;
        DateTimeOffset? sweepCutoff = command.Scope == LinkThreadScope.EntireThread ? null : snapshot.ReceivedAt;

        var tagged = await threadTagger.TagThreadAsync(
            command.MessageId, snapshot.InternetMessageId, sweepConversationId,
            TriageCategories.Admin, cancellationToken, anchorReceivedAt: sweepCutoff);
        if (!tagged)
            throw new InvalidOperationException(
                "The KPI is recorded, but the email couldn't be tagged out of the queue. Apply again to retry the tag.");

        var hasPathway = (snapshot.Categories ?? Array.Empty<string>()).Any(TriageCategories.IsBucketTag);
        if (hasPathway) return;
        try
        {
            await threadTagger.TagThreadAsync(
                command.MessageId, snapshot.InternetMessageId, sweepConversationId,
                TriageCategories.Internal, cancellationToken, anchorReceivedAt: sweepCutoff);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { /* best-effort — the Admin tag already holds it out of the queue */ }
    }

    private static string Clamp(string? value, int max) =>
        value is null ? "" : value.Length <= max ? value : value[..max];
}
