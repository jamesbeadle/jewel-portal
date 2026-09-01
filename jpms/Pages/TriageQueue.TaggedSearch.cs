using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- Tagged tab search ---------------------------------------------------------------------

    private void OnTaggedSearchInput(ChangeEventArgs e)
    {
        taggedSearch = e.Value?.ToString() ?? "";
        var query = taggedSearch.Trim();
        if (query == taggedSearchPending) return;
        taggedSearchPending = query;
        taggedSearchDebounce?.Cancel();
        if (query.Length < 2)
        {
            // Typed back below the threshold: drop out of search mode but keep the box's text —
            // the user may still be typing.
            _ = ResetTaggedSearchModeAsync();
            return;
        }
        var cts = taggedSearchDebounce = new CancellationTokenSource();
        _ = RunTaggedSearchAsync(query, cts.Token);
    }

    // The ✕ button: empty the box and fall back to the ordinary filtered list.
    private async Task ClearTaggedSearchAsync()
    {
        taggedSearchDebounce?.Cancel();
        taggedSearch = "";
        taggedSearchPending = "";
        await ResetTaggedSearchModeAsync();
    }

    // Leave search mode: clear its state and, if a resolved tag was filtering the server read,
    // reload the ordinary list. Selection is left alone — an email opened from the search stays
    // open, exactly as it does when a tag filter is cleared.
    private async Task ResetTaggedSearchModeAsync()
    {
        taggedSearching = false;
        taggedSearchResults = null;
        taggedSearchRecord = null;
        var hadTagFilter = taggedSearchTag is not null;
        taggedSearchTag = null;
        if (hadTagFilter)
        {
            ResetTaggedPaging();
            await LoadTaggedAsync();
        }
        StateHasChanged();
    }

    private async Task RunTaggedSearchAsync(string query, CancellationToken token)
    {
        try { await Task.Delay(500, token); } catch (TaskCanceledException) { return; }
        taggedSearching = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            // A reference-shaped query (one token with a dash) is first offered to the tag
            // resolver — the same ResolveRecordTags behind the to-do search's chips. A hit turns
            // the search into that record's exact server-side tag filter, so paging, selection
            // and the tags pane all behave exactly as with the dropdown filter.
            if (!query.Contains(' ') && query.Contains('-'))
            {
                var records = await Queries.AskAsync(new ResolveRecordTags(new[] { query }), token);
                if (token.IsCancellationRequested) return;
                if (records.FirstOrDefault() is LinkableRecord record)
                {
                    taggedSearchRecord = record;
                    taggedSearchTag = $"JPMS/{record.TagReference}";
                    taggedSearchResults = null;
                    ResetTaggedPaging();
                    taggedSearching = false;
                    await LoadTaggedAsync();
                    return;
                }
            }
            // Otherwise free-text: one relevance-ordered page of the whole mailbox. Untagged
            // matches are INCLUDED, marked as still-in-the-queue — "find that past email" is the
            // question being asked, and an email hidden for not being tagged yet is exactly the
            // one that needs finding (selecting it opens it for tagging like any queue email).
            var found = await Queries.AskAsync(new SearchMailboxMessages(query, 25), token);
            if (token.IsCancellationRequested) return;
            taggedSearchRecord = null;
            taggedSearchTag = null;
            taggedSearchResults = found;
        }
        catch (OperationCanceledException) { }
        catch
        {
            if (token.IsCancellationRequested) return;
            taggedSearchResults = Array.Empty<MailboxMessage>();
            loadError = "The mailbox couldn't be searched just then. Try again in a moment.";
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                taggedSearching = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    // --------------------------------------------------------------------------------------------

    // Tick/untick a tag in the multi-select filter, then re-read the (OR-filtered) list from page one.
    // Using the tag filter drops any active pathway chip — the two can't be intersected server-side.
    private async Task ToggleTag(string tag)
    {
        if (!selectedTags.Add(tag)) selectedTags.Remove(tag);
        pathwayBucketFilter = null;
        ParkSelectedTriage();
        selected = null;
        detail = null;
        detailLoading = false;
        ResetTaggedPaging();
        await LoadTaggedAsync();
    }

    private async Task ClearTagFilters()
    {
        selectedTags.Clear();
        filterOpen = false;
        ParkSelectedTriage();
        selected = null;
        detail = null;
        detailLoading = false;
        ResetTaggedPaging();
        await LoadTaggedAsync();
    }

    // Pick a pathway chip (null = All). Clears the record-tag filter for the same OR-vs-AND reason —
    // the chips and the tag dropdown are two lenses on the same server-side category filter.
    private async Task SetPathwayFilter(string? bucket)
    {
        if (pathwayBucketFilter == bucket) return;
        pathwayBucketFilter = bucket;
        selectedTags.Clear();
        filterOpen = false;
        ParkSelectedTriage();
        selected = null;
        detail = null;
        detailLoading = false;
        ResetTaggedPaging();
        await LoadTaggedAsync();
    }

    private async Task PreviousTagged()
    {
        if (taggedIndex <= 0) return;
        taggedIndex--;
        await LoadTaggedAsync();
    }

    private async Task NextTagged()
    {
        if (taggedNext is null) return;
        taggedIndex++;
        await LoadTaggedAsync();
    }

    private async Task LoadUnassignedAsync()
    {
        unassignedError = null;
        try { unassigned = await RequestRegister.ListUnassignedAsync(); }
        catch { unassigned = Array.Empty<Request>(); }
        finally { unassignedArrived = true; }
    }

    private async Task ReturnUnassigned(Request request)
    {
        if (busy) return;
        unassignedError = null;
        try
        {
            busy = true;
            await RequestRegister.ReturnToTriageAsync(request.RequestId, request.ProjectId);
            await LoadAsync();
            await LoadUnassignedAsync();
        }
        catch
        {
            unassignedError = "Couldn't return that request to the Control Centre. Please try again.";
        }
        finally { busy = false; }
    }

}
