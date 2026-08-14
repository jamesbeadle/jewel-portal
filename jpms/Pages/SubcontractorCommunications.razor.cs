using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Pages;

public partial class SubcontractorCommunications
{
    private const int PageSize = 25;

    // Session checked and the user signed in — the chrome shows straight away; `loaded` is the
    // separate question of whether the page of emails has arrived (the loading-states convention).
    private bool sessionReady;
    private bool loaded;
    private bool loadingMore;
    private string? loadError;

    private readonly List<MailboxMessage> items = new();
    private int total;
    private string? nextCursor;

    // The email a Reply or Forward was pressed on (the shared composer opens above the list;
    // sending from this page sends immediately), which of the two it was, and the confirmation
    // left behind by the last send.
    private MailboxMessage? replyTo;
    private bool composeIsForward;
    private string? replySent;

    private void StartCompose(MailboxMessage message, bool forward)
    {
        replyTo = message;
        composeIsForward = forward;
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        StateHasChanged();
        // The project list only feeds the reply composer's attachment picker — losing it costs
        // the picker its drawing/photo sources, not the page.
        _ = LoadProjectListAsync();
        await LoadPageAsync(cursor: null);
    }

    private async Task LoadProjectListAsync()
    {
        try { await ProjectList.RefreshAsync(CancellationToken.None); }
        catch { /* reported by the query client; the picker's project sources render empty */ }
    }

    private async Task OnReplySent(Jewel.JPMS.Contracts.MailboxCompose.ComposeOutcome outcome)
    {
        var wasForward = composeIsForward;
        replyTo = null;
        composeIsForward = false;
        replySent = outcome.Sent
            ? $"{(wasForward ? "Forward" : "Reply")} sent to {string.Join("; ", outcome.To)} — it joins the thread and files back into this list."
            : $"The {(wasForward ? "forward" : "reply")} was saved to the mailbox's Drafts — review and send it from Outlook.";
        // The sent copy self-files by tag; re-read the first page so it appears straight away.
        await LoadPageAsync(cursor: null);
    }

    // One live read of the tagged mail, newest first — the same query the Control Centre's Tagged
    // view uses, narrowed to the one communication tag.
    private async Task LoadPageAsync(string? cursor)
    {
        loadError = null;
        try
        {
            var page = await Intake.ListTaggedLiveAsync(
                cursor, PageSize, new[] { SubcontractorComms.Tag }, newestFirst: true);
            if (cursor is null) items.Clear();
            items.AddRange(page.Items);
            total = page.Total;
            nextCursor = page.NextCursor;
        }
        catch
        {
            loadError = "Couldn't load the communications. Please try again.";
        }
        finally
        {
            loaded = true;
        }
    }

    private async Task LoadMoreAsync()
    {
        if (nextCursor is null || loadingMore) return;
        loadingMore = true;
        try { await LoadPageAsync(nextCursor); }
        finally { loadingMore = false; }
    }
}
