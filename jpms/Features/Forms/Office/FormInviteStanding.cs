namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>How the Sent out screen narrows the forms sent on their own: still waiting, done, or gone stale.</summary>
public static class FormInviteStanding
{
    public const string Waiting = "waiting";
    public const string Done = "done";
    public const string Stale = "stale";
    public const string Everything = "all";

    public static bool IsWaiting(FormInvite invite, DateTimeOffset now) =>
        invite.StateAt(now) is FormLinkState.NotOpened or FormLinkState.Opened;

    public static IReadOnlyList<TabItem> Chips(IReadOnlyList<FormInvite>? invites, DateTimeOffset now) => new[]
    {
        new TabItem(Waiting, "Waiting", Count: invites?.Count(invite => IsWaiting(invite, now))),
        new TabItem(Done, "Done"),
        new TabItem(Stale, "Expired or replaced"),
        new TabItem(Everything, "Everything")
    };

    public static IReadOnlyList<FormInvite> Apply(IEnumerable<FormInvite> invites, string filter, DateTimeOffset now) =>
        invites.Where(invite => IsIn(invite.StateAt(now), filter)).ToList();

    private static bool IsIn(FormLinkState state, string filter) => filter switch
    {
        Waiting => state is FormLinkState.NotOpened or FormLinkState.Opened,
        Done => state == FormLinkState.Done,
        Stale => state is FormLinkState.Expired or FormLinkState.Replaced,
        _ => true
    };
}
