using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>Writes the expiry and public liability figure onto the CURRENT version of a document.
/// A superseded version is history and is refused; the file, kind and version number never
/// change here — a different certificate is a new version through the upload routes.</summary>
public sealed class SetComplianceDocumentDetailsHandler
    : ICommandHandler<SetComplianceDocumentDetails, ComplianceDocument>
{
    private readonly JpmsContext context;

    public SetComplianceDocumentDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<ComplianceDocument> HandleAsync(SetComplianceDocumentDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.ComplianceDocuments
            .FirstOrDefaultAsync(row => row.ComplianceDocumentId == command.ComplianceDocumentId, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Compliance document {command.ComplianceDocumentId} not found.");
        if (!string.Equals(entity.SubcontractorId, command.SubcontractorId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("That document belongs to a different company.");
        if (entity.SupersededAt is not null)
            throw new InvalidOperationException("That version has been superseded — the current version is the one to correct.");

        var isANewExpiry = entity.ExpiresAt != command.ExpiresAt;
        entity.ExpiresAt = command.ExpiresAt;
        entity.PublicLiabilityCover = command.PublicLiabilityCover;
        if (isANewExpiry) RestartTheRenewalChase(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    /// <summary>A corrected expiry is a renewal the forms' chase has not asked for yet, so it starts again.</summary>
    private static void RestartTheRenewalChase(ComplianceDocumentEntity entity)
    {
        entity.LastChasedAt = null;
        entity.ChaseCount = 0;
    }
}
