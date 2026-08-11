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
        entity.DepositReleasedOpening = command.DepositReleasedOpening;

        // The deposit AND retention terms ride on each claim (stamped at claim start,
        // frozen when it locks). A Draft is still live, so a terms change flows straight
        // onto any open drafts — that's what lets terms be introduced or corrected
        // mid-period (the Ravenswood/Woodhouse reconciliations: a claim opened before
        // terms existed would otherwise carry 0% forever) without restarting the open
        // claim. Locked claims keep their frozen copy. The completion release % follows
        // the same rule StartValuationClaimHandler applies: it only bites once the
        // claim date has reached practical completion.
        var draftClaims = await context.ValuationClaims
            .Where(claim => claim.ProjectId == command.ProjectId
                            && claim.Status == (int)ValuationClaimStatus.Draft)
            .ToListAsync(cancellationToken);
        foreach (var draft in draftClaims)
        {
            draft.RetentionPercent = command.RetentionPercent;
            draft.RetentionReleasePercent =
                command.PracticalCompletionAt is { } practicalCompletion && draft.ClaimDate >= practicalCompletion
                    ? command.CompletionReleasePercent
                    : 0m;
            draft.DepositPercent = command.DepositPercent;
            draft.DepositReleasedOpening = command.DepositReleasedOpening;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
