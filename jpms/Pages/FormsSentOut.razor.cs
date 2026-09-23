using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Features.Forms.Office;

namespace Jewel.JPMS.Pages;

/// <summary>
/// Forms sent on their own (the dashboard's Sent out): who each link went to, who sent it, whether
/// it has been opened or used, and the two things the office does from the row — send it again (a
/// fresh link; the old one dies, so a forwarded email cannot be used later) or cancel it.
/// </summary>
public partial class FormsSentOut
{
    private readonly FormWrite write = new();
    private IReadOnlyList<FormInvite>? invites;
    private bool dataFailed;
    private bool isSending;
    private FormInvite? resending;
    private FormInvite? cancelling;
    private SentLinkOutcome? sent;
    private string filter = FormInviteStanding.Waiting;
    private DateTimeOffset now = DateTimeOffset.UtcNow;

    private IReadOnlyList<FormInvite> Rows => FormInviteStanding.Apply(invites ?? Array.Empty<FormInvite>(), filter, now);

    private IReadOnlyList<TabItem> Chips => FormInviteStanding.Chips(invites, now);

    private string EmptyMessage => dataFailed
        ? "Couldn't load the links — refresh to try again."
        : "No links here. Send a form emails one person a link that is theirs alone.";

    protected override async Task OnInitializedAsync()
    {
        var mayRead = await Entry.MayReadAsync(FormRoleSets.Office);
        if (mayRead) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        now = DateTimeOffset.UtcNow;
        try { invites = await Queries.AskAsync(new ListFormInvites(), CancellationToken.None); }
        catch { dataFailed = true; }
    }

    private IReadOnlyList<DropdownMenu.Item> MenuFor(FormInvite invite) => new DropdownMenu.Item[]
    {
        new("Send it again…", EventCallback.Factory.Create(this, () => resending = invite),
            Hint: "A fresh link and a fresh expiry; the old link stops working.", Disabled: invite.UsedAt is not null),
        new("Cancel the link…", EventCallback.Factory.Create(this, () => cancelling = invite),
            Destructive: true, Disabled: !FormInviteStanding.IsWaiting(invite, now))
    };

    private async Task LinkSentAsync(SentLinkOutcome outcome)
    {
        isSending = false;
        resending = null;
        sent = outcome;
        await LoadAsync();
    }

    private async Task CancelAsync()
    {
        var invite = cancelling!;
        var isCancelled = await write.RunAsync(() => Commands.SendAsync(new CancelFormInvite(invite.FormInviteId), CancellationToken.None));
        cancelling = null;
        if (isCancelled) await LoadAsync();
    }
}
