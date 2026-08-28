using System.Text.Json;
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

    public static WeeklyCashflowSupplierGroup ToModel(this WeeklyCashflowSupplierGroupEntity entity) => new(
        SupplierGroupId: entity.SupplierGroupId,
        Name: entity.Name,
        ContactNames: ReadContactNames(entity.ContactNamesJson),
        CreatedByEmail: entity.CreatedByEmail,
        CreatedAt: entity.CreatedAt);

    public static WeeklyCashflowExclusion ToModel(this WeeklyCashflowExclusionEntity entity) => new(
        PlacementKey: entity.PlacementKey,
        ExcludedByEmail: entity.ExcludedByEmail,
        ExcludedAt: entity.ExcludedAt);

    public static string WriteContactNames(IReadOnlyList<string> contactNames) =>
        JsonSerializer.Serialize(contactNames);

    /// <summary>A stored group whose JSON won't parse yields an empty member list rather than a
    /// failed plan read — the group simply groups nothing until it is re-saved.</summary>
    private static IReadOnlyList<string> ReadContactNames(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
