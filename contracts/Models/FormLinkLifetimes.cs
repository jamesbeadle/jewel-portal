namespace Jewel.JPMS.Models;

/// <summary>
/// How long links live. A single form's link lasts the days the office picks — seven unless told
/// otherwise, never more than sixty. A pack lasts fourteen days from sending, and whenever the
/// person opens it or sends one of its forms it is kept alive for at least another seven, because
/// a pack that dies while somebody is half-way through is worse than no pack — but never beyond
/// sixty days from sending. Sending a pack again puts a fresh fourteen days on a new link.
/// </summary>
public static class FormLinkLifetimes
{
    public const int DefaultInviteDays = 7;
    public const int LongestDays = 60;
    public static readonly int[] InviteDayChoices = { 3, 7, 14, 30 };
    public static readonly TimeSpan PackOnSending = TimeSpan.FromDays(14);
    public static readonly TimeSpan PackAfterActivity = TimeSpan.FromDays(7);

    public static int InviteDays(int requested) =>
        requested < 1 ? DefaultInviteDays : Math.Min(requested, LongestDays);

    public static DateTimeOffset PackKeptAliveAt(DateTimeOffset expiresAt, DateTimeOffset sentAt, DateTimeOffset now)
    {
        var keptAlive = now + PackAfterActivity;
        var latest = sentAt.AddDays(LongestDays);
        var capped = keptAlive < latest ? keptAlive : latest;
        return capped > expiresAt ? capped : expiresAt;
    }
}
