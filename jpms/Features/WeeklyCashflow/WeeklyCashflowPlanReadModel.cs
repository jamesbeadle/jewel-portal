using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Features.WeeklyCashflow;

/// <summary>
/// The Weekly Cashflow's stored plan — manual items and placements. Current is null until the
/// first fetch lands (the honest "not fetched yet"; the page gates on it). After a command the
/// page APPLIES the server's answer here rather than refetching: every command returns the row
/// as stored, so the local plan stays exact and the grid answers instantly.
/// </summary>
public sealed class WeeklyCashflowPlanReadModel
{
    private readonly IQueryClient queries;

    public WeeklyCashflowPlanReadModel(IQueryClient queries) { this.queries = queries; }

    public WeeklyCashflowPlan? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new GetWeeklyCashflowPlan(), cancellationToken);
        OnChanged?.Invoke();
    }

    /// <summary>Folds a created or updated item into the plan; an archived item leaves it (the
    /// grid never shows archived items, matching the query).</summary>
    public void Apply(WeeklyCashflowItem item)
    {
        if (Current is null) return;
        var items = Current.Items.Where(existing => existing.WeeklyCashflowItemId != item.WeeklyCashflowItemId);
        if (item.ArchivedAt is null) items = items.Append(item);
        Current = Current with { Items = items.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase).ToList() };
        OnChanged?.Invoke();
    }

    /// <summary>Folds a placement answer into the plan — the stored row, or null for a cleared
    /// placement (the entry falls back to its natural week).</summary>
    public void Apply(string placementKey, WeeklyCashflowPlacement? placement)
    {
        if (Current is null) return;
        var placements = Current.Placements.Where(existing => existing.PlacementKey != placementKey);
        if (placement is not null) placements = placements.Append(placement);
        Current = Current with { Placements = placements.ToList() };
        OnChanged?.Invoke();
    }

    /// <summary>Folds a saved supplier group into the plan — created or renamed, the server's
    /// row replaces whatever the plan held for its id.</summary>
    public void Apply(WeeklyCashflowSupplierGroup group)
    {
        if (Current is null) return;
        var groups = Current.SupplierGroups
            .Where(existing => existing.SupplierGroupId != group.SupplierGroupId)
            .Append(group)
            .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Current = Current with { SupplierGroups = groups };
        OnChanged?.Invoke();
    }

    /// <summary>Removes a dissolved supplier group from the plan.</summary>
    public void RemoveGroup(string supplierGroupId)
    {
        if (Current is null) return;
        Current = Current with
        {
            SupplierGroups = Current.SupplierGroups
                .Where(existing => existing.SupplierGroupId != supplierGroupId)
                .ToList()
        };
        OnChanged?.Invoke();
    }

    /// <summary>Folds an exclusion answer into the plan — the stored row, or null for a lifted
    /// exclusion (the entry counts again).</summary>
    public void ApplyExclusion(string placementKey, WeeklyCashflowExclusion? exclusion)
    {
        if (Current is null) return;
        var exclusions = Current.Exclusions.Where(existing => existing.PlacementKey != placementKey);
        if (exclusion is not null) exclusions = exclusions.Append(exclusion);
        Current = Current with { Exclusions = exclusions.ToList() };
        OnChanged?.Invoke();
    }
}
