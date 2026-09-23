using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Features.Forms.Office;

namespace Jewel.JPMS.Pages;

/// <summary>
/// New starter packs — the one screen of done and outstanding (the goal's measure): each person's
/// forms with where every one has got to, and the chase from the row, which sends the one link
/// again with a fresh fourteen days while what is done stays done.
/// </summary>
public partial class FormPacks
{
    private readonly FormWrite write = new();
    private IReadOnlyList<FormPack>? packs;
    private bool dataFailed;
    private bool isSending;
    private FormPack? cancelling;
    private SentLinkOutcome? sent;
    private string filter = FormPackStanding.Waiting;
    private DateTimeOffset now = DateTimeOffset.UtcNow;

    private IReadOnlyList<FormPack> Rows => FormPackStanding.Apply(packs ?? Array.Empty<FormPack>(), filter);

    private IReadOnlyList<TabItem> Chips => FormPackStanding.Chips(packs);

    private string EmptyMessage => dataFailed
        ? "Couldn't load the packs — refresh to try again."
        : "No packs here. Send a pack gives a new starter one link to every form they owe.";

    protected override async Task OnInitializedAsync()
    {
        var mayRead = await Entry.MayReadAsync(FormRoleSets.Office);
        if (mayRead) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        now = DateTimeOffset.UtcNow;
        try { packs = await Queries.AskAsync(new ListFormPacks(), CancellationToken.None); }
        catch { dataFailed = true; }
    }

    private IReadOnlyList<DropdownMenu.Item> MenuFor(FormPack pack) => new DropdownMenu.Item[]
    {
        new("Chase — email the link again", EventCallback.Factory.Create(this, () => ChaseAsync(pack)),
            Hint: "A new link with a fresh fourteen days; what is done stays done.", Disabled: !FormPackStanding.IsWaiting(pack)),
        new("Cancel the pack…", EventCallback.Factory.Create(this, () => cancelling = pack),
            Destructive: true, Disabled: !FormPackStanding.IsWaiting(pack))
    };

    private async Task PackSentAsync(SentLinkOutcome outcome)
    {
        isSending = false;
        sent = outcome;
        await LoadAsync();
    }

    private async Task ChaseAsync(FormPack pack)
    {
        var chased = await write.RunAsync(async () =>
            sent = SentLinkOutcome.Of(await Commands.SendAsync(new ChaseFormPack(pack.FormPackId), CancellationToken.None)));
        if (chased) await LoadAsync();
    }

    private async Task CancelAsync()
    {
        var pack = cancelling!;
        var isCancelled = await write.RunAsync(() => Commands.SendAsync(new CancelFormPack(pack.FormPackId), CancellationToken.None));
        cancelling = null;
        if (isCancelled) await LoadAsync();
    }
}
