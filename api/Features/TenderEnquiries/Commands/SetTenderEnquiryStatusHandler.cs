using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>
/// Sets where an enquiry stands. Any move goes, forwards or back (James, 2026-08-25: a wrong
/// press must be undoable) — the handler stamps the date a move means (PQQ submitted, tender
/// submitted, decided) and clears the stamps a step back makes untrue. Winning also moves the
/// project on from Lead to Pre-Construction — the reference was minted when the enquiry was
/// logged, so nothing else changes hands.
/// </summary>
public sealed class SetTenderEnquiryStatusHandler : ICommandHandler<SetTenderEnquiryStatus, TenderEnquiry>
{
    private const string ClientPathwayLabel = "Client";

    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public SetTenderEnquiryStatusHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<TenderEnquiry> HandleAsync(SetTenderEnquiryStatus command, CancellationToken cancellationToken)
    {
        var entity = await context.TenderEnquiries
            .FirstOrDefaultAsync(row => row.TenderEnquiryId == command.TenderEnquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Tender enquiry '{command.TenderEnquiryId}' not found.");

        var current = (TenderEnquiryStatus)entity.Status;
        if (current == command.Status)
            throw new InvalidOperationException($"The enquiry is already {command.Status.DisplayName().ToLowerInvariant()}.");

        Stamp(entity, command);
        if (command.Status == TenderEnquiryStatus.Won) await MoveProjectOnFromLeadAsync(entity.ProjectId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            AuditEventType.TenderEnquiryStatusChanged,
            $"{entity.ToModel().Reference} moved from {current.DisplayName()} to {command.Status.DisplayName()}"
                + (string.IsNullOrWhiteSpace(command.Note) ? "." : $" — {command.Note.Trim()}"),
            pathway: ClientPathwayLabel,
            projectId: entity.ProjectId,
            recordType: RecordType.TenderEnquiry,
            recordId: entity.TenderEnquiryId,
            recordReference: entity.ToModel().Reference,
            cancellationToken: cancellationToken);
        return entity.ToModel();
    }

    // The submitted stamps follow the journey: a step forward sets the one it reaches (first time
    // only, so a re-set keeps the original date); a step back to before it clears it.
    private static void Stamp(TenderEnquiryEntity entity, SetTenderEnquiryStatus command)
    {
        var now = DateTimeOffset.UtcNow;
        entity.Status = (int)command.Status;
        if (command.Status == TenderEnquiryStatus.PqqSubmitted) entity.PqqSubmittedAt ??= now;
        if (command.Status == TenderEnquiryStatus.TenderSubmitted) entity.TenderSubmittedAt ??= now;
        if (command.Status == TenderEnquiryStatus.Received) entity.PqqSubmittedAt = null;
        if (command.Status is TenderEnquiryStatus.Received or TenderEnquiryStatus.PqqSubmitted or TenderEnquiryStatus.Shortlisted)
            entity.TenderSubmittedAt = null;
        if (command.Status.IsOpen())
        {
            entity.DecidedAt = null;
            entity.DecisionNote = "";
            return;
        }
        entity.DecidedAt = now;
        entity.DecisionNote = TenderEnquiryDetailsRules.Clamp(command.Note, 2048);
    }

    // Only a project still at Lead moves — a job already under way keeps whatever stage it has.
    private async Task MoveProjectOnFromLeadAsync(string projectId, CancellationToken cancellationToken)
    {
        var project = await context.Projects.FirstOrDefaultAsync(row => row.ProjectId == projectId, cancellationToken);
        if (project is null || project.Stage != (int)ProjectStage.Lead) return;
        project.Stage = (int)ProjectStage.PreConstruction;
    }
}
