using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits;

/// <summary>The shared rules of an audit's header and items — validation and the entity
/// writes — so create, update and the connector cannot drift apart.</summary>
internal static class HsAuditRules
{
    private const int NameLength = 256;
    private const int TextLength = 4000;
    private const int FindingsLength = 2000;

    public static IReadOnlyList<string> DetailProblems(HsAuditDetails details)
    {
        var errors = new List<string>();
        if (details.InspectionDate == default) errors.Add("Give the inspection date.");
        if (details.SafetyOfficerName is { Length: > NameLength }) errors.Add("Safety officer name must be 256 characters or fewer.");
        if (details.SiteManagerName is { Length: > NameLength }) errors.Add("Site manager name must be 256 characters or fewer.");
        if (details.SummaryOfWorkActivities is { Length: > TextLength }) errors.Add("Summary of work activities must be 4000 characters or fewer.");
        if (details.FurtherComments is { Length: > TextLength }) errors.Add("Further comments must be 4000 characters or fewer.");
        if (details.SiteOperativeCount is < 0) errors.Add("Site operatives can't be negative.");
        return errors;
    }

    public static IReadOnlyList<string> ItemProblems(IReadOnlyList<HsAuditItemEntry> entries)
    {
        var errors = new List<string>();
        if (entries.Count == 0) errors.Add("Name at least one item.");
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.HsAuditItemId)) errors.Add("Every item needs its HsAuditItemId.");
            if (entry.Minus < 0) errors.Add($"Item {entry.HsAuditItemId}: minus can't be negative.");
            if (entry.Findings is { Length: > FindingsLength }) errors.Add($"Item {entry.HsAuditItemId}: findings must be 2000 characters or fewer.");
            if (entry.OwnerName is { Length: > NameLength }) errors.Add($"Item {entry.HsAuditItemId}: owner must be 256 characters or fewer.");
        }
        return errors;
    }

    public static void Apply(HsAuditEntity entity, HsAuditDetails details)
    {
        entity.Type = (int)details.Type;
        entity.InspectionDate = AsCalendarDate(details.InspectionDate);
        entity.SiteManagerName = details.SiteManagerName?.Trim() ?? "";
        entity.SafetyOfficerName = details.SafetyOfficerName?.Trim() ?? "";
        entity.SummaryOfWorkActivities = details.SummaryOfWorkActivities?.Trim() ?? "";
        entity.SiteOperativeCount = details.SiteOperativeCount;
        entity.FurtherComments = details.FurtherComments?.Trim() ?? "";
    }

    public static void Apply(HsAuditItemEntity entity, HsAuditItemEntry entry)
    {
        entity.Comment = entry.Comment is { } comment ? (int)comment : null;
        entity.Rate = entry.Rate is { } rate ? (int)rate : null;
        entity.Class = entry.Class is { } hsAuditClass ? (int)hsAuditClass : null;
        entity.Minus = entry.Minus;
        entity.TimeScale = entry.TimeScale is { } timeScale ? (int)timeScale : null;
        entity.Findings = entry.Findings?.Trim() ?? "";
        entity.OwnerName = entry.OwnerName?.Trim() ?? "";
        entity.DateRectified = AsCalendarDate(entry.DateRectified);
    }

    /// <summary>The audit's score, recomputed from every item (HsAuditScoring is the one rule).</summary>
    public static decimal? ScoreOf(IEnumerable<HsAuditItemEntity> items) =>
        HsAuditScoring.ScoreOf(items.Select(item => item.ToModel()));

    public static bool IsEditable(HsAuditEntity entity) => entity.Status != (int)HsAuditStatus.Closed;

    public static DateTimeOffset AsCalendarDate(DateTimeOffset value) => new(value.UtcDateTime.Date, TimeSpan.Zero);

    public static DateTimeOffset? AsCalendarDate(DateTimeOffset? value) =>
        value is { } date ? AsCalendarDate(date) : null;
}
