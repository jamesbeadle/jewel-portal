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

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        StateHasChanged();
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
