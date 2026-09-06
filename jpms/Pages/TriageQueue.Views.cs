using Jewel.JPMS.Features.Triage.Queue;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    private int? InboxTotal => view switch
    {
        QueueView.Active => queueArrived ? total : null,
        QueueView.Discarded => discardedArrived ? discardedTotal : null,
        _ => taggedArrived ? taggedTotal : null,
    };

    private string SelectPrompt => view switch
    {
        QueueView.Active => "Select an email to process it.",
        QueueView.Discarded => "Select a discarded email to view it.",
        _ => "Select a tagged email to manage its tags.",
    };

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        RequestRegister.OnChange += StateHasChanged;
        workspace.OnChange += StateHasChanged;
        // The sort preference has to land first — LoadAsync pages the mailbox in this order, so
        // reading it late would fetch the first page the wrong way round. It is a local-storage
        // read, so it costs almost nothing.
        newestFirst = await SortStorage.ReadNewestFirstAsync(Auth.CurrentUser!.Email);
        // Paint the chrome before the four fetches: Blazor re-renders OnInitializedAsync only at
        // its FIRST await, which has already passed. The sort toggle is drawn from newestFirst, so
        // it goes out with the rest rather than flipping under the cursor a moment later.
        StateHasChanged();

        // The remaining four are independent of each other; issued together the page waits once
        // for the slowest instead of for the sum. Triage is the heaviest page in the app to open.
        //
        // Every one of them has to be non-throwing. Task.WhenAll rethrows the first failure, and an
        // exception escaping OnInitializedAsync takes the whole page down to the error boundary —
        // which is exactly what a failing project list used to do here, turning one bad read into a
        // dead triage queue. The other three already swallowed their own failures; the project list
        // was the odd one out. The error still reaches the toast, because the query client reports
        // it to the error sink before rethrowing, so nothing is hidden by catching it here.
        await Task.WhenAll(
            LoadProjectsAsync(),
            LoadAsync(),
            LoadUnassignedAsync(),
            LoadTodoAssignableRolesAsync(),
            LoadRecentTriageAsync());

        // A finder elsewhere (the to-do searches' email results) may have sent one specific email
        // here to be opened — select THAT, on the pile it lives in, instead of the default landing.
        if (OpenEmail.Take() is MailboxMessage handedOver)
        {
            if (handedOver.Categories.Count > 0 || handedOver.Bucket is not null)
                await SwitchView(QueueView.Tagged);
            await Select(handedOver);
            return;
        }

        // Land straight in the first email: opening the top of the queue is what a triager does
        // first every time, so the page does it for them. Initial load only — after an action the
        // deliberately-cleared pane is where the outcome banners (reply draft, partial link) show.
        if (view != QueueView.Active || selected is not null || items.Count == 0) return;
        await Select(items[0]);
    }

    // The project list only feeds the "file this email against a project" pickers. Losing it should
    // cost the pickers their options, not cost the triager the entire queue.
    private async Task LoadProjectsAsync()
    {
        try { await Projects.RefreshAsync(CancellationToken.None); }
        catch { /* reported by the query client; the pickers render empty */ }
    }

    private async Task SwitchView(QueueView next)
    {
        if (view == next) return;
        ParkSelectedTriage();
        view = next;
        selected = null;
        detail = null;
        detailLoading = false;
        discardArmed = false;
        stagedCreate = null;
        relevantEventStaged = null;
        triageEntireThread = null;
        useThreadTags = null;
        pickedRecords.Clear();
        actionError = null;
        composeOutcome = null;
        if (next == QueueView.Discarded) { ResetDiscardedPaging(); await LoadDiscardedAsync(); }
        else if (next == QueueView.Tagged)
        {
            selectedTags.Clear(); pathwayBucketFilter = null; filterOpen = false;
            // Entering the tab starts from the unfiltered pile — the search resets with the rest.
            taggedSearchDebounce?.Cancel();
            taggedSearch = ""; taggedSearchPending = ""; taggedSearching = false;
            taggedSearchRecord = null; taggedSearchTag = null; taggedSearchResults = null;
            ResetTaggedPaging(); await LoadTaggedAsync();
        }
        else { ResetQueuePaging(); await Task.WhenAll(LoadAsync(), LoadRecentTriageAsync()); }
    }

    // Flip the sort order, remember the choice, and re-read the visible list from page one (an
    // offset cursor from one order is meaningless in the other). Selection is cleared like a view
    // switch: the previously open email may not even be on the new first page.
    private async Task SetSortAsync(bool newest)
    {
        if (newestFirst == newest) return;
        newestFirst = newest;
        if (Auth.CurrentUser is not null)
            await SortStorage.WriteAsync(Auth.CurrentUser.Email, newest);
        ParkSelectedTriage();
        selected = null;
        detail = null;
        detailLoading = false;
        discardArmed = false;
        stagedCreate = null;
        relevantEventStaged = null;
        triageEntireThread = null;
        useThreadTags = null;
        pickedRecords.Clear();
        actionError = null;
        composeOutcome = null;
        if (view == QueueView.Discarded) { ResetDiscardedPaging(); await LoadDiscardedAsync(); }
        else if (view == QueueView.Tagged) { ResetTaggedPaging(); await LoadTaggedAsync(); }
        else { ResetQueuePaging(); await LoadAsync(); }
    }

    private void ResetQueuePaging() { queueCursors = new() { null }; queueIndex = 0; queueNext = null; }
    private void ResetDiscardedPaging() { discardedCursors = new() { null }; discardedIndex = 0; discardedNext = null; }
    private void ResetTaggedPaging() { taggedCursors = new() { null }; taggedIndex = 0; taggedNext = null; }

    private async Task PreviousPage()
    {
        if (queueIndex <= 0) return;
        queueIndex--;
        await LoadAsync();
    }

    private async Task NextPage()
    {
        if (queueNext is null) return;
        queueIndex++;
        await LoadAsync();
    }

    private async Task PreviousDiscarded()
    {
        if (discardedIndex <= 0) return;
        discardedIndex--;
        await LoadDiscardedAsync();
    }

    private async Task NextDiscarded()
    {
        if (discardedNext is null) return;
        discardedIndex++;
        await LoadDiscardedAsync();
    }

    private void ToggleFilterMenu() => filterOpen = !filterOpen;
}
