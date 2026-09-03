using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Api.Features.Kpi.Commands;

// Marks a mailbox email as a KPI against a person. The email is read back from the mailbox (so a
// stale queue row cannot mark something that has since gone) and its envelope snapshotted onto
// the row; NOTHING in the mailbox is tagged — the mark is invisible to everyone triaging the
// queue, by design (the register is administrators-only). One mark per email per person:
// marking the same email for the same person twice answers with the existing row rather than a
// twin (a new note replaces the old).
public sealed class MarkEmailAsKpiHandler : ICommandHandler<MarkEmailAsKpi, KpiEmail>
{
    private readonly JpmsContext context;
    private readonly KpiPersonResolver people;
    private readonly IMailboxGraphClient graph;
    private readonly AuditTrail audit;

    public MarkEmailAsKpiHandler(JpmsContext context, KpiPersonResolver people, IMailboxGraphClient graph, AuditTrail audit)
    { this.context = context; this.people = people; this.graph = graph; this.audit = audit; }

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

        return entity.ToModel(person);
    }

    private static string Clamp(string? value, int max) =>
        value is null ? "" : value.Length <= max ? value : value[..max];
}
