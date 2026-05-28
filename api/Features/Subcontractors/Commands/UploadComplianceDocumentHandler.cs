using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UploadComplianceDocumentHandler
    : ICommandHandler<UploadComplianceDocument, ComplianceDocument>
{
    private readonly JpmsContext context;

    public UploadComplianceDocumentHandler(JpmsContext context) { this.context = context; }

    public async Task<ComplianceDocument> HandleAsync(UploadComplianceDocument command, CancellationToken cancellationToken)
    {
        var entity = new ComplianceDocumentEntity
        {
            ComplianceDocumentId = SubcontractorIdentifierFactory.NextComplianceDocumentId(),
            SubcontractorId = command.SubcontractorId,
            Kind = command.Kind,
            FileName = command.FileName,
            ExpiresAt = command.ExpiresAt,
            UploadedAt = DateTimeOffset.UtcNow
        };
        context.ComplianceDocuments.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
