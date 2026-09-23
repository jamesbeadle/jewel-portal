namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// How the right-to-work register reads a check, in the dashboard's words: its expiry with the days
/// left, whether it is cleared, not finished or a do-not-start, and the register's one-line summary
/// of what needs a look.
/// </summary>
public static class RightToWorkStanding
{
    public const string Current = "current";
    public const string Left = "left";
    public const string Everything = "all";

    public static (string Label, Tone Tone) ExpiryOf(RightToWorkCheckDetails details, DateOnly today)
    {
        if (details.PermissionExpiresOn is null && !details.IsTimeLimited) return ("No time limit", Tone.Muted);
        if (details.PermissionExpiresOn is not { } expiry) return ("Time-limited, no date recorded", Tone.Negative);
        var daysLeft = expiry.DayNumber - today.DayNumber;
        var tone = daysLeft < 0 ? Tone.Negative : daysLeft <= RightToWorkRules.FollowUpDaysBeforeExpiry ? Tone.Warning : Tone.Positive;
        var left = daysLeft < 0 ? $"expired {-daysLeft}d ago" : $"{daysLeft}d left";
        return ($"{DateFormats.DateText(expiry)} · {left}", tone);
    }

    public static (string Label, Tone Tone) OutcomeOf(RightToWorkCheckDetails details) => details switch
    {
        _ when RightToWorkRules.IsDoNotStart(details) => ("Do not start", Tone.Negative),
        _ when RightToWorkRules.IsCleared(details) => ("Cleared", Tone.Positive),
        _ => ("Not finished", Tone.Warning)
    };

    public static IReadOnlyList<TabItem> Chips(IReadOnlyList<RightToWorkCheck>? checks) => new[]
    {
        new TabItem(Current, "Current", Count: checks?.Count(check => check.EngagementEndedOn is null)),
        new TabItem(Left, "Left"),
        new TabItem(Everything, "Everything")
    };

    public static IReadOnlyList<RightToWorkCheck> Apply(IEnumerable<RightToWorkCheck> checks, string filter) =>
        checks.Where(check => filter switch
        {
            Current => check.EngagementEndedOn is null,
            Left => check.EngagementEndedOn is not null,
            _ => true
        }).ToList();

    public static string? NeedsALook(IReadOnlyList<RightToWorkCheck> checks, DateOnly today)
    {
        var current = checks.Where(check => check.EngagementEndedOn is null).Select(check => check.Details).ToList();
        var parts = new[]
        {
            Counted(current.Count(IsUnfinished), "check not finished", "checks not finished"),
            Counted(current.Count(details => IsExpiring(details, today)), "expiring or expired", "expiring or expired"),
            Counted(current.Count(RightToWorkRules.IsDoNotStart), "recorded as do not start", "recorded as do not start"),
            Counted(current.Count(RightToWorkRules.IsLate), "late check", "late checks")
        }.Where(part => part is not null).ToList();
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static bool IsUnfinished(RightToWorkCheckDetails details) =>
        !RightToWorkRules.IsDoNotStart(details) && RightToWorkRules.GapsIn(details).Count > 0;

    private static bool IsExpiring(RightToWorkCheckDetails details, DateOnly today) =>
        details.PermissionExpiresOn is { } expires
            ? expires.DayNumber - today.DayNumber <= RightToWorkRules.FollowUpDaysBeforeExpiry
            : details.IsTimeLimited;

    private static string? Counted(int count, string one, string many) =>
        count == 0 ? null : $"{count} {(count == 1 ? one : many)}";
}
