namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>How the training register narrows: the certificates that need renewing first, then the rest.</summary>
public static class TrainingStandingFilters
{
    public const string Renewing = "renewing";
    public const string Current = "current";
    public const string Left = "left";
    public const string Everything = "all";

    public static bool NeedsRenewing(TrainingRecord record, DateOnly today) =>
        record.StandingOn(today) is TrainingStanding.Expired or TrainingStanding.ExpiringSoon;

    public static IReadOnlyList<TabItem> Chips(IReadOnlyList<TrainingRecord>? records, DateOnly today) => new[]
    {
        new TabItem(Renewing, "Needs renewing", Count: records?.Count(record => NeedsRenewing(record, today))),
        new TabItem(Current, "Current"),
        new TabItem(Left, "Left"),
        new TabItem(Everything, "Everything")
    };

    public static IReadOnlyList<TrainingRecord> Apply(IEnumerable<TrainingRecord> records, string filter, DateOnly today) =>
        records.Where(record => filter switch
        {
            Renewing => NeedsRenewing(record, today),
            Current => record.EndedOn is null,
            Left => record.EndedOn is not null,
            _ => true
        }).OrderBy(record => record.ExpiresOn ?? DateOnly.MaxValue).ToList();

    public static string ChaseText(TrainingRecord record) =>
        record.LastChasedAt is { } chased ? $"{record.ChaseCount}× · {DateFormats.DateText(chased)}" : "—";
}
