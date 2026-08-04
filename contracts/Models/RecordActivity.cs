namespace Jewel.JPMS.Models;

// How much correspondence has landed on a record recently — the band behind the activity badge.
// None renders nothing at all: a quiet record must not carry a grey chip saying it is quiet.
public enum ActivityBand
{
    None = 0,   // quiet — nothing rendered
    Recent = 1, // something in the last week, now fading
    Active = 2, // roughly an email today, or a few this week
    Busy = 3    // several emails in the last day or two — probably one to click into
}

/// <summary>
/// The one shared reading of a record's communication activity. Emails linked at triage decay with
/// a half-life: an email linked just now scores 1.0, three days ago 0.5, a week ago ~0.2 — so the
/// score is high exactly when a lot has arrived recently, and melts away on its own when the record
/// goes quiet. Kept out of the api handler and the UI alike (the RequestDates pattern) so the side
/// that computes the score and the side that colours the badge can never drift apart.
/// </summary>
public static class ActivityScore
{
    /// <summary>Days for one linked email's contribution to halve.</summary>
    public const double HalfLifeDays = 3;

    /// <summary>Events older than this contribute ~0.04 — nothing — so the window also bounds the
    /// query that feeds the score.</summary>
    public const int WindowDays = 14;

    // Band floors. A single email older than ~6 days falls below Recent — a one-off from last
    // week is not "activity". Tuning happens here and nowhere else.
    public const double RecentThreshold = 0.25;
    public const double ActiveThreshold = 1.0;
    public const double BusyThreshold = 3.0;

    /// <summary>Σ over events of 2^(−ageDays / half-life), within the window.</summary>
    public static double For(IEnumerable<DateTimeOffset> eventTimes, DateTimeOffset now)
    {
        double score = 0;
        foreach (var occurredAt in eventTimes)
        {
            var ageDays = (now - occurredAt).TotalDays;
            if (ageDays < 0) ageDays = 0;           // clock skew — never let the future score extra
            if (ageDays > WindowDays) continue;
            score += Math.Pow(2, -ageDays / HalfLifeDays);
        }
        return score;
    }

    public static ActivityBand BandFor(double score) =>
        score >= BusyThreshold ? ActivityBand.Busy
        : score >= ActiveThreshold ? ActivityBand.Active
        : score >= RecentThreshold ? ActivityBand.Recent
        : ActivityBand.None;
}

/// <summary>
/// One record's recent triage activity: how many emails were linked to it in the last 7 days (the
/// hover text), when the most recent one landed, and the decayed score the badge's band derives
/// from. Reference is denormalised from the audit rows so a consumer can render without joins.
/// Note CountLast7Days can be 0 while the band is still Recent — several emails just over a week
/// old can hold the score above the floor together.
/// </summary>
public sealed record RecordActivitySummary(
    RecordType Type,
    string RecordId,
    string Reference,
    int CountLast7Days,
    DateTimeOffset LastAt,
    double Score)
{
    public ActivityBand Band => ActivityScore.BandFor(Score);
}
