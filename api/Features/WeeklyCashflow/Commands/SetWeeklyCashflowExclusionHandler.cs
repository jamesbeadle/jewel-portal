using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

/// <summary>Upserts (Excluded = true) or deletes (false) the one exclusion row for an entry.
/// Excluding again refreshes the stamp — the newest word wins, same as placements. The answer
/// is enveloped — never a bare null, which would leave the endpoint as a bodiless 204
/// (JPMS-31996D).</summary>
public sealed class SetWeeklyCashflowExclusionHandler : ICommandHandler<SetWeeklyCashflowExclusion, WeeklyCashflowExclusionAnswer>
{
    private readonly JpmsContext context;

    public SetWeeklyCashflowExclusionHandler(JpmsContext context) { this.context = context; }

    public async Task<WeeklyCashflowExclusionAnswer> HandleAsync(SetWeeklyCashflowExclusion command, CancellationToken cancellationToken)
    {
        var entity = await context.WeeklyCashflowExclusions
            .FirstOrDefaultAsync(exclusion => exclusion.PlacementKey == command.PlacementKey, cancellationToken);

        if (!command.Excluded)
        {
            if (entity is not null)
            {
                context.WeeklyCashflowExclusions.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            }
            return new WeeklyCashflowExclusionAnswer(null);
        }

        if (entity is null)
        {
            entity = new WeeklyCashflowExclusionEntity { PlacementKey = command.PlacementKey };
            context.WeeklyCashflowExclusions.Add(entity);
        }
        entity.ExcludedByEmail = command.ExcludedByEmail;
        entity.ExcludedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return new WeeklyCashflowExclusionAnswer(entity.ToModel());
    }
}
