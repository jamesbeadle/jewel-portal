namespace Jewel.JPMS.Api.Features.Manual.Queries;

/// <summary>How many people have acknowledged each version of each module, read in one query.</summary>
internal sealed class ManualAcknowledgementCounts
{
    private readonly IReadOnlyDictionary<(string ModuleId, int Version), int> counts;

    private ManualAcknowledgementCounts(IReadOnlyDictionary<(string, int), int> counts) { this.counts = counts; }

    public static async Task<ManualAcknowledgementCounts> ForPublishedVersionsAsync(JpmsContext context, CancellationToken cancellationToken)
    {
        var rows = await context.ManualAcknowledgements
            .Select(row => new { row.ManualModuleId, row.Version })
            .ToListAsync(cancellationToken);
        var counts = rows.GroupBy(row => (row.ManualModuleId, row.Version)).ToDictionary(group => group.Key, group => group.Count());
        return new ManualAcknowledgementCounts(counts);
    }

    public static async Task<ManualAcknowledgementCounts> ForModuleAsync(JpmsContext context, string manualModuleId, CancellationToken cancellationToken)
    {
        var rows = await context.ManualAcknowledgements
            .Where(row => row.ManualModuleId == manualModuleId)
            .GroupBy(row => row.Version)
            .Select(group => new { Version = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        return new ManualAcknowledgementCounts(rows.ToDictionary(row => (manualModuleId, row.Version), row => row.Count));
    }

    public int Of(string manualModuleId, int version) =>
        counts.TryGetValue((manualModuleId, version), out var count) ? count : 0;
}
