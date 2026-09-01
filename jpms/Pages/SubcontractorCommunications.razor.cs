using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class SubcontractorCommunications
{
    private const int PageSize = 25;

    /// <summary>The register deep link's category segment ("chaser", "materials", "site-instruction"
    /// …) — resolved against the route's family; blank or unknown shows the whole family.</summary>
    [Microsoft.AspNetCore.Components.Parameter] public string? Category { get; set; }

    // Which record-less family this page is showing — decided by the route (the one component
    // serves /subcontractors/communications and /internal/communications).
    private CommunicationFamily family = CommunicationFamily.Subcontractor;

    // Session checked and the user signed in — the chrome shows straight away; `loaded` is the
    // separate question of whether the page of emails has arrived (the loading-states convention).
    private bool sessionReady;
    private bool loaded;
    private bool loadingMore;
    private string? loadError;

    private readonly List<MailboxMessage> items = new();
    private int total;
    private string? nextCursor;

    // The category filter (2026-08-17): null = the whole family (general + every category), or one
    // family tag ("JPMS/SubComms", "JPMS/SubComms-Chase", …) from the chip row. The chips read the
    // family straight from the contracts constant, so a new category is one line there.
    private string? categoryTagFilter;

    private IReadOnlyList<string> TagsToRead =>
        categoryTagFilter is null
            ? family.Tags
            : new[] { categoryTagFilter };

    // The tag a listed email's chip row may omit: it is this list's premise. Under "All" only the
    // general tag is implied — a category tag on a card is information, not noise.
    private string ImpliedTag => categoryTagFilter ?? family.Tag;

    // Switching chip resets the list BEFORE the fetch: a failed re-query must not leave the old
    // filter's emails (or its Graph cursor — "Load more" would mix filters) under the new chip.
    private async Task SetCategoryFilterAsync(string? tag)
    {
        if (categoryTagFilter == tag) return;
        categoryTagFilter = tag;
        loaded = false;
        items.Clear();
        total = 0;
        nextCursor = null;
        await LoadPageAsync(cursor: null);
    }

    private string ActiveFilterLabel =>
        family.All
            .Where(record => CommunicationFamily.TagFor(record) == categoryTagFilter)
            .Select(family.ChipLabel)
            .FirstOrDefault() ?? "";

    private string ChipClass(string? tag) =>
        "rounded-full border px-3 py-1 text-xs font-medium transition "
        + (categoryTagFilter == tag
            ? "border-accent bg-accent/10 text-accent"
            : "border-line text-content-muted hover:text-content hover:border-line-strong");

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
        family = CommunicationFamily.ForRoute(new Uri(Nav.Uri).AbsolutePath);
        if (family.ForSlug(Category) is { } routedCategory)
            categoryTagFilter = CommunicationFamily.TagFor(routedCategory);
        sessionReady = true;
        StateHasChanged();
        // The project list only feeds the reply composer's attachment picker — losing it costs
        // the picker its drawing/photo sources, not the page.
        _ = LoadProjectListAsync();
        await LoadPageAsync(cursor: null);
    }

    // Blazor keeps this one component instance when the user moves between its two routes, so the
    // family is re-read on every parameter set and the list reloaded when it has changed.
    protected override async Task OnParametersSetAsync()
    {
        if (!sessionReady) return;
        var routed = CommunicationFamily.ForRoute(new Uri(Nav.Uri).AbsolutePath);
        var routedFilter = routed.ForSlug(Category) is { } routedCategory
            ? CommunicationFamily.TagFor(routedCategory)
            : null;
        if (routed == family && routedFilter == categoryTagFilter) return;
        family = routed;
        categoryTagFilter = routedFilter;
        loaded = false;
        items.Clear();
        total = 0;
        nextCursor = null;
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

    // Two quick chip clicks race their fetches; whichever response lands LAST would win, chip or
    // not. The sequence number says which request is current — a stale response changes nothing.
    private int loadSequence;

    // One live read of the tagged mail, newest first — the same query the Control Centre's Tagged
    // view uses, narrowed to the communication family (or the one category the chip row picked).
    private async Task LoadPageAsync(string? cursor)
    {
        var sequence = ++loadSequence;
        loadError = null;
        try
        {
            var page = await Intake.ListTaggedLiveAsync(
                cursor, PageSize, TagsToRead, newestFirst: true);
            if (sequence != loadSequence) return;
            if (cursor is null) items.Clear();
            items.AddRange(page.Items);
            total = page.Total;
            nextCursor = page.NextCursor;
        }
        catch
        {
            if (sequence != loadSequence) return;
            loadError = "Couldn't load the communications. Please try again.";
        }
        finally
        {
            if (sequence == loadSequence) loaded = true;
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
