using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

/// <summary>Upserts (or, with a null week, deletes) the one placement row for an entry. The
/// stored week is normalised to its Monday, so whatever a client sends the grid's arithmetic
/// and the stored plan agree on the same axis.</summary>
public sealed class PlaceWeeklyCashflowEntryHandler : ICommandHandler<PlaceWeeklyCashflowEntry, WeeklyCashflowPlacement?>
{
    private readonly JpmsContext context;

    public PlaceWeeklyCashflowEntryHandler(JpmsContext context) { this.context = context; }

    public async Task<WeeklyCashflowPlacement?> HandleAsync(PlaceWeeklyCashflowEntry command, CancellationToken cancellationToken)
    {
        var entity = await context.WeeklyCashflowPlacements
            .FirstOrDefaultAsync(placement => placement.PlacementKey == command.PlacementKey, cancellationToken);

        if (command.PlannedWeekStart is not { } plannedWeek)
        {
            if (entity is not null)
            {
                context.WeeklyCashflowPlacements.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            }
            return null;
        }

        var weekStart = WeeklyCashflowMaths.WeekStartFor(plannedWeek);
        if (entity is null)
        {
            entity = new WeeklyCashflowPlacementEntity { PlacementKey = command.PlacementKey };
            context.WeeklyCashflowPlacements.Add(entity);
        }
        entity.PlannedWeekStart = weekStart;
        entity.MovedByEmail = command.MovedByEmail;
        entity.MovedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
