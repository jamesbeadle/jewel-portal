namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>A pack's standing as the office reads it, and the packs screen's narrowing: the ones still owed forms first.</summary>
public static class FormPackStanding
{
    public const string Waiting = "waiting";
    public const string Complete = "complete";
    public const string Everything = "all";

    public static (string Label, Tone Tone) Of(FormPack pack, DateTimeOffset now) => pack switch
    {
        { CancelledAt: not null } => ("Cancelled", Tone.Muted),
        { CompletedAt: not null } => ("Complete", Tone.Positive),
        _ when pack.ExpiresAt < now => ("Link expired", Tone.Warning),
        { OpenedAt: not null } => ("Opened", Tone.Info),
        _ => ("Not opened", Tone.Muted)
    };

    public static bool IsWaiting(FormPack pack) => pack.CancelledAt is null && pack.CompletedAt is null;

    public static IReadOnlyList<TabItem> Chips(IReadOnlyList<FormPack>? packs) => new[]
    {
        new TabItem(Waiting, "Waiting on forms", Count: packs?.Count(IsWaiting)),
        new TabItem(Complete, "Complete"),
        new TabItem(Everything, "Everything")
    };

    public static IReadOnlyList<FormPack> Apply(IEnumerable<FormPack> packs, string filter) => packs.Where(pack => filter switch
    {
        Waiting => IsWaiting(pack),
        Complete => pack.CompletedAt is not null,
        _ => true
    }).ToList();

    public static string ChaseText(FormPack pack) =>
        pack.LastChasedAt is { } chased ? $"{pack.ChaseCount}× · {DateFormats.DateText(chased)}" : "—";
}
