using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>
/// Writes the enquiry row itself — numbered on the global TEQ sequence, Received, owned by whoever
/// logged it — and records the logging in the audit trail. Shared by the from-email and by-hand
/// commands so both mint the same way.
/// </summary>
public sealed class TenderEnquiryRegister
{
    private const string ClientPathwayLabel = "Client";

    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public TenderEnquiryRegister(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<TenderEnquiryEntity> LogAsync(
        string projectId, TenderEnquiryDetails details, string loggedByEmail, CancellationToken cancellationToken)
    {
        var entity = new TenderEnquiryEntity
        {
            TenderEnquiryId = TenderEnquiryIdentifierFactory.Next(),
            ProjectId = projectId,
            Number = await NextNumberAsync(cancellationToken),
            Status = (int)TenderEnquiryStatus.Received,
            OwnerEmail = loggedByEmail,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByEmail = loggedByEmail
        };
        TenderEnquiryDetailsRules.Apply(entity, details);
        context.TenderEnquiries.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>Best-effort, after the save — the AuditTrail convention.</summary>
    public Task RecordLoggedAsync(TenderEnquiryEntity entity, bool createdProject, CancellationToken cancellationToken)
    {
        var projectNote = createdProject ? " — a new Lead-stage project was created for it" : "";
        return audit.WriteAsync(
            AuditEventType.TenderEnquiryLogged,
            $"Tender enquiry {entity.ToModel().Reference} logged from {entity.ArchitectPracticeName}: \"{entity.Title}\"{projectNote}.",
            pathway: ClientPathwayLabel,
            projectId: entity.ProjectId,
            recordType: RecordType.TenderEnquiry,
            recordId: entity.TenderEnquiryId,
            recordReference: entity.ToModel().Reference,
            cancellationToken: cancellationToken);
    }

    private async Task<int> NextNumberAsync(CancellationToken cancellationToken) =>
        (await context.TenderEnquiries.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;
}
