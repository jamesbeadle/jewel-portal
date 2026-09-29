using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// The one rail every batch of claim entries passes — recording them on a Draft and restating
/// them on a locked claim alike. Any number of decimals, positive or negative: a % is whatever
/// reproduces the claimed value on the line, and ±100000 is only a typo rail. The entry count
/// has generous headroom over the largest real bill (Abbot Road runs ~250 lines).
/// </summary>
internal static class ClaimEntryRail
{
    private const int MaxEntries = 2000;
    private const decimal PercentRail = 100000m;

    public static IEnumerable<string> Errors(IReadOnlyList<ClaimEntryInput>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            yield return "At least one entry is required.";
            yield break;
        }
        var hasTooMany = entries.Count > MaxEntries;
        var hasAnUnnamedLine = entries.Any(IsUnnamed);
        var hasAPercentOffTheRail = entries.Any(IsOffTheRail);
        var hasARepeatedLine = entries.GroupBy(entry => entry.ValuationLineItemId).Any(group => group.Count() > 1);
        if (hasTooMany) yield return $"At most {MaxEntries} entries per request.";
        if (hasAnUnnamedLine) yield return "Every entry needs a ValuationLineItemId.";
        if (hasAPercentOffTheRail) yield return $"Percent complete must be between -{PercentRail}% and {PercentRail}% on every entry.";
        if (hasARepeatedLine) yield return "Each line may appear only once per request.";
    }

    private static bool IsUnnamed(ClaimEntryInput entry) => string.IsNullOrWhiteSpace(entry.ValuationLineItemId);

    private static bool IsOffTheRail(ClaimEntryInput entry) =>
        entry.PercentComplete < -PercentRail || entry.PercentComplete > PercentRail;
}
