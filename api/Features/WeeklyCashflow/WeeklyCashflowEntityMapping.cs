using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow;

internal static class WeeklyCashflowEntityMapping
{
    public static WeeklyCashflowItem ToModel(this WeeklyCashflowItemEntity entity) => new(
        WeeklyCashflowItemId: entity.WeeklyCashflowItemId,
        Name: entity.Name,
        Category: (WeeklyCashflowCategory)entity.Category,
        Amount: entity.Amount,
        Recurrence: (WeeklyCashflowRecurrence)entity.Recurrence,
        FirstDueOn: entity.FirstDueOn,
        LastDueOn: entity.LastDueOn,
        Notes: entity.Notes,
        CreatedByEmail: entity.CreatedByEmail,
        CreatedAt: entity.CreatedAt,
        ArchivedAt: entity.ArchivedAt);

    public static WeeklyCashflowPlacement ToModel(this WeeklyCashflowPlacementEntity entity) => new(
        PlacementKey: entity.PlacementKey,
        PlannedWeekStart: entity.PlannedWeekStart,
        MovedByEmail: entity.MovedByEmail,
        MovedAt: entity.MovedAt);
}
