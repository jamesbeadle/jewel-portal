using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class RecordSubcontractorRetentionHandler
    : ICommandHandler<RecordSubcontractorRetention, SubcontractorRetention>
{
    private readonly JpmsContext context;

    public RecordSubcontractorRetentionHandler(JpmsContext context) { this.context = context; }

    public async Task<SubcontractorRetention> HandleAsync(RecordSubcontractorRetention command, CancellationToken cancellationToken)
    {
        var entity = await context.SubcontractorRetentions.FirstOrDefaultAsync(
            retention => retention.ProjectId == command.ProjectId
                && retention.SubcontractorReference == command.SubcontractorReference,
            cancellationToken);
        if (entity is null)
        {
            entity = new SubcontractorRetentionEntity
            {
                SubcontractorRetentionId = CommercialInputsIdentifierFactory.NextSubcontractorRetentionId(),
                ProjectId = command.ProjectId,
                SubcontractorReference = command.SubcontractorReference
            };
            context.SubcontractorRetentions.Add(entity);
        }
        entity.CertifiedAmount = command.CertifiedAmount;
        entity.RetentionPercent = command.RetentionPercent;
        entity.FirstReleasedAmount = command.FirstReleasedAmount;
        entity.FinalReleasedAmount = command.FinalReleasedAmount;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
