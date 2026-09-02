namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // After an action consumes an email, its list shrinks under the pager. The cursor is a plain
    // offset (see MailboxGraphClient.ListFilteredAsync), so re-reading the SAME page simply refills
    // it from the emails further down — the triager stays on the page they were working, and emails
    // they deliberately skipped stay behind on earlier pages instead of being re-presented from
    // page one after every action. Only when the page has fallen off the end entirely (the last
    // email on the last page was consumed) does this step back — one page at a time, never past
    // page one. A failed reload keeps the index rather than guessing: loadError already tells the
    // story, and Previous/Next still work. View switches and sort flips still reset to page one —
    // those genuinely start a new read of the list.
    private async Task ReloadQueueInPlaceAsync()
    {
        await LoadAsync();
        while (loadError is null && items.Count == 0 && queueIndex > 0)
        {
            queueIndex--;
            await LoadAsync();
        }
    }

    private async Task ReloadDiscardedInPlaceAsync()
    {
        await LoadDiscardedAsync();
        while (loadError is null && discardedItems.Count == 0 && discardedIndex > 0)
        {
            discardedIndex--;
            await LoadDiscardedAsync();
        }
    }

    // The Discarded tab's one action: un-discard the open email, back into the live queue.
    private async Task DoRestore()
    {
        if (selected is null || busy) return;
        actionError = null;
        try
        {
            busyLabel = "Restoring";
            busy = true;
            await Intake.RestoreMessageAsync(selected.Id, selected.InternetMessageId);
            selected = null;
            detail = null;
            detailLoading = false;
            ReturnWorkspaceToQueue();
            await ReloadDiscardedInPlaceAsync();
        }
        catch
        {
            actionError = "Couldn't restore that email. Please try again.";
        }
        finally { busy = false; }
    }

    private async Task ReloadTaggedInPlaceAsync()
    {
        await LoadTaggedAsync();
        while (loadError is null && taggedItems.Count == 0 && taggedIndex > 0)
        {
            taggedIndex--;
            await LoadTaggedAsync();
        }
    }

    private async Task LoadAsync()
    {
        loadError = null;
        listLoading = true;
        // Paint the spinner before the fetch: Blazor only re-renders an event handler at its FIRST
        // await, so when a caller awaited something else first (SetSortAsync persists the choice to
        // localStorage before reloading), setting listLoading here would otherwise never render and
        // the list sits still until the new page lands.
        StateHasChanged();
        try
        {
            var result = await Intake.ListInboxLiveAsync(queueCursors[queueIndex], PageSize, newestFirst);
            items = result.Items;
            total = result.Total;
            queueNext = result.NextCursor;
            // Record the cursor for the next page so Next can advance to it.
            if (queueNext is not null && queueIndex == queueCursors.Count - 1)
                queueCursors.Add(queueNext);
        }
        catch
        {
            loadError = "Couldn't load the inbox. Please try again.";
            items = Array.Empty<MailboxMessage>();
            total = 0;
            queueNext = null;
        }
        finally
        {
            listLoading = false;
            queueArrived = true;
        }
    }

    private async Task LoadDiscardedAsync()
    {
        loadError = null;
        listLoading = true;
        StateHasChanged(); // paint the spinner before the fetch — see LoadAsync
        try
        {
            var result = await Intake.ListDiscardedLiveAsync(discardedCursors[discardedIndex], PageSize, newestFirst);
            discardedItems = result.Items;
            discardedTotal = result.Total;
            discardedNext = result.NextCursor;
            if (discardedNext is not null && discardedIndex == discardedCursors.Count - 1)
                discardedCursors.Add(discardedNext);
        }
        catch
        {
            loadError = "Couldn't load discarded emails. Please try again.";
            discardedItems = Array.Empty<MailboxMessage>();
            discardedTotal = 0;
            discardedNext = null;
        }
        finally
        {
            listLoading = false;
            discardedArrived = true;
        }
    }

    private async Task LoadTaggedAsync()
    {
        loadError = null;
        listLoading = true;
        StateHasChanged(); // paint the spinner before the fetch — see LoadAsync
        try
        {
            // The search's resolved record tag, the pathway chip and the record-tag multi-select
            // are mutually exclusive (see the pathwayBucketFilter note), so exactly one of them
            // feeds the server's tags filter — the search first, since the others render disabled
            // while it is live.
            var filter = taggedSearchTag is not null
                ? new List<string> { taggedSearchTag }
                : pathwayBucketFilter is not null
                    ? new List<string> { pathwayBucketFilter }
                    : selectedTags.Count == 0 ? null : selectedTags.ToList();
            var result = await Intake.ListTaggedLiveAsync(taggedCursors[taggedIndex], PageSize, filter, newestFirst);
            taggedItems = result.Items;
            taggedTotal = result.Total;
            taggedNext = result.NextCursor;
            if (taggedNext is not null && taggedIndex == taggedCursors.Count - 1)
                taggedCursors.Add(taggedNext);
            // Remember every tag we see so the filter dropdown can offer them.
            foreach (var message in taggedItems)
                foreach (var tag in message.Categories)
                    knownTags.Add(tag);
        }
        catch
        {
            loadError = "Couldn't load tagged emails. Please try again.";
            taggedItems = Array.Empty<MailboxMessage>();
            taggedTotal = 0;
            taggedNext = null;
        }
        finally
        {
            listLoading = false;
            taggedArrived = true;
        }
    }
}
