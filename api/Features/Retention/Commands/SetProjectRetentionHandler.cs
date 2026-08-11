using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Retention.Commands;

// Upsert — a project has at most one retention record. Editing terms never touches the
// confirmed release state; a confirmed release's frozen amount stays frozen.
public sealed class SetProjectRetentionHandler : ICommandHandler<SetProjectRetention, ProjectRetention>
{
    private readonly JpmsContext context;

    public SetProjectRetentionHandler(JpmsContext context) { this.context = context; }

    public async Task<ProjectRetention> HandleAsync(SetProjectRetention command, CancellationToken cancellationToken)
    {
        var entity = await context.ProjectRetentions.FirstOrDefaultAsync(
            retention => retention.ProjectId == command.ProjectId, cancellationToken);
        if (entity is null)
        {
            entity = new ProjectRetentionEntity
            {
                ProjectRetentionId = RetentionIdentifierFactory.NextProjectRetentionId(),
                ProjectId = command.ProjectId
            };
            context.ProjectRetentions.Add(entity);
        }

        entity.RetentionPercent = command.RetentionPercent;
        entity.CompletionReleasePercent = command.CompletionReleasePercent;
        entity.DefectsPeriodMonths = command.DefectsPeriodMonths;
        entity.PracticalCompletionAt = command.PracticalCompletionAt;
        entity.DepositPercent = command.DepositPercent;

        // The deposit % rides on each claim (stamped at claim start, frozen when it locks).
        // A Draft is still live, so a terms change flows straight onto any open drafts —
        // that's what lets a deposit be introduced mid-period (the Ravenswood reconciliation)
        // without restarting the open claim. Locked claims keep their frozen copy.
        var draftClaims = await context.ValuationClaims
            .Where(claim => claim.ProjectId == command.ProjectId
                            && claim.Status == (int)ValuationClaimStatus.Draft)
            .ToListAsync(cancellationToken);
        foreach (var draft in draftClaims)
            draft.DepositPercent = command.DepositPercent;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
