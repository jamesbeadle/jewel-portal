using Jewel.JPMS.Features.Triage;
using static Jewel.JPMS.Features.Triage.RecordLinkVocabulary;

namespace Jewel.JPMS.Pages;

public partial class ProjectCommunications
{
    [Parameter] public string ProjectId { get; set; } = "";

    private const int PageSize = 25;

    // Session checked and the user is signed in — the filters and the tab chrome show straight
    // away. `loaded` is the separate question of whether the page of emails has arrived.
    private bool loaded;
    private bool loadingMore;
    private string? loadError;

    private string typeFilterRaw = "";
    private RecordType? TypeFilter =>
        Enum.TryParse<RecordType>(typeFilterRaw, out var parsed) ? parsed : null;

    // The pathway segmented control's selection: a short bucket label ("Client"), or null for all.
    private string? bucketFilter;
    private static readonly string[] BucketLabels = { "Client", "Subcontractor", "Supplier", "Internal" };

    private readonly List<ProjectCommunication> items = new();
    private string? nextCursor;
    private int total;

    // Search within the project's tagged mail. searchText mirrors the box; activeSearch is the
    // query the current list answers (null = the ordinary paged read). Debounced like the Control
    // Centre's Tagged-tab search: 500ms, two characters minimum.
    private string searchText = "";
    private string? activeSearch;
    private string searchPending = "";
    private CancellationTokenSource? searchDebounce;
    private bool SearchActive => activeSearch is not null;

    // The inline "Add tag" picker: open on at most one row at a time. Type first, then one of
    // this project's records of that type (the Control Centre's link trio minus the project
    // pick — the page IS the project).
    private string? tagPickerMessageId;
    private RecordType tagRecordType = RecordTypeOptions[0];
    private IReadOnlyList<LinkableRecord> tagRecords = Array.Empty<LinkableRecord>();
    private bool tagRecordsLoading;
    private string tagRecordId = "";
    private bool tagBusy;
    private string? tagError;
    private (string MessageId, string Text)? tagAdded;

    // Linking is a triage power (the api's LinkMessageToRecord gate) — mirrors
    // TaggedEmailSearch.CanSearchMailbox / TriageQueue.CanTriage; keep the three in step.
    private bool CanTag =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector
            or Role.ProjectManager or Role.FinanceDirector);

    // The email a Reply or Forward was pressed on (the shared composer opens above the list;
    // sending from a record page sends immediately), which of the two it was, and the
    // confirmation left behind by the last send.
    private MailboxMessage? replyTo;
    private bool composeIsForward;
    private string? replySent;

    private void StartCompose(MailboxMessage message, bool forward)
    {
        replyTo = message;
        composeIsForward = forward;
    }

    private async Task OnReplySent(Jewel.JPMS.Contracts.MailboxCompose.ComposeOutcome outcome)
    {
        var wasForward = composeIsForward;
        replyTo = null;
        composeIsForward = false;
        replySent = outcome.Sent
            ? $"{(wasForward ? "Forward" : "Reply")} sent to {string.Join("; ", outcome.To)} — it joins the thread and files back into this list."
            : $"The {(wasForward ? "forward" : "reply")} was saved to the mailbox's Drafts — review and send it from Outlook.";
        // The sent copy self-files by the thread's tags; re-read so it appears straight away.
        await LoadAsync(reset: true);
    }

    private async Task LoadProjectListAsync()
    {
        try { await ProjectList.RefreshAsync(CancellationToken.None); }
        catch { /* reported by the query client; the picker's project sources render empty */ }
    }

    // Every linkable record type, labelled in canonical UI terms — RecordType.Scheduling is the
    // programme's bucket, so it reads "Relevant Events" here (never "Scheduling" in UI copy;
    // "Relevant Event" per the 2026-08-07 rename).
    private static readonly (RecordType Type, string Label)[] TypeOptions =
    {
        (RecordType.CostCentre,       "Cost centres"),
        (RecordType.Request,          "Requests (RFI/RFA/…)"),
        (RecordType.BidPackageInvite, "Bid package invites"),
        (RecordType.WorkOrder,        "Work orders"),
        (RecordType.Scheduling,       "Relevant events (Programme)"),
        (RecordType.Todo,             "To-dos"),
        (RecordType.CalendarEvent,    "Calendar events"),
        (RecordType.Lad,              "LADs"),
        (RecordType.Variation,        "Variation orders"),
        (RecordType.VariationQuote,   "VO quotes"),
        (RecordType.Defect,           "Defects"),
        (RecordType.Inventory,        "Inventory"),
        (RecordType.SiteInstruction,  "Site instructions"),
        (RecordType.ValuationClaim,   "Valuation claims"),
        (RecordType.ValuationReportSnapshot, "Valuation snapshots")
    };

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        // The project list only feeds the reply composer's attachment picker — background warm.
        _ = LoadProjectListAsync();
        await LoadAsync(reset: true);
    }

    private async Task OnTypeFilterChanged(ChangeEventArgs e)
    {
        typeFilterRaw = e.Value?.ToString() ?? "";
        await LoadAsync(reset: true);
    }

    private async Task OnBucketChanged(string? bucket)
    {
        if (bucketFilter == bucket) return;
        bucketFilter = bucket;
        await LoadAsync(reset: true);
    }

    private string BucketTabClass(string? bucket) =>
        bucketFilter == bucket
            ? "btn-primary text-xs px-2.5 py-1.5"
            : "btn-secondary text-xs px-2.5 py-1.5";

    private Task LoadMoreAsync() => LoadAsync(reset: false);

    // ---- Search ------------------------------------------------------------------------------

    private void OnSearchInput(ChangeEventArgs e)
    {
        searchText = e.Value?.ToString() ?? "";
        var query = searchText.Trim();
        if (query == searchPending) return;
        searchPending = query;
        searchDebounce?.Cancel();
        if (query.Length < 2)
        {
            // Typed back below the threshold: leave search mode (reloading the paged list if a
            // search was showing) but keep the box's text — the user may still be typing.
            if (SearchActive) _ = ClearSearchResultsAsync();
            return;
        }
        var cts = searchDebounce = new CancellationTokenSource();
        _ = RunSearchAsync(query, cts.Token);
    }

    private async Task ClearSearchAsync()
    {
        searchDebounce?.Cancel();
        searchText = "";
        searchPending = "";
        await ClearSearchResultsAsync();
    }

    private async Task ClearSearchResultsAsync()
    {
        activeSearch = null;
        await LoadAsync(reset: true);
        StateHasChanged();
    }

    private async Task RunSearchAsync(string query, CancellationToken token)
    {
        try { await Task.Delay(500, token); } catch (TaskCanceledException) { return; }
        if (token.IsCancellationRequested) return;
        activeSearch = query;
        await LoadAsync(reset: true);
        if (token.IsCancellationRequested) return;
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose() => searchDebounce?.Cancel();

    // ---- Add tag -----------------------------------------------------------------------------

    private bool IsTagPickerOpen(MailboxMessage message) => tagPickerMessageId == message.Id;

    private void ToggleTagPicker(MailboxMessage message)
    {
        if (tagBusy) return;
        if (IsTagPickerOpen(message))
        {
            tagPickerMessageId = null;
            return;
        }
        tagPickerMessageId = message.Id;
        tagError = null;
        tagAdded = null;
        tagRecordId = "";
        // Keep the last chosen type and its loaded records across rows — tagging several emails
        // to the same kind of record is the common run; only the first open fetches.
        if (tagRecords.Count == 0 && !tagRecordsLoading) _ = LoadTagRecordsAsync();
    }

    private async Task OnTagTypeChanged(ChangeEventArgs e)
    {
        if (!Enum.TryParse<RecordType>(e.Value?.ToString(), out var type) || type == tagRecordType) return;
        tagRecordType = type;
        await LoadTagRecordsAsync();
    }

    private void OnTagRecordChanged(ChangeEventArgs e) => tagRecordId = e.Value?.ToString() ?? "";

    private async Task LoadTagRecordsAsync()
    {
        tagRecordsLoading = true;
        tagRecordId = "";
        tagRecords = Array.Empty<LinkableRecord>();
        tagError = null;
        StateHasChanged();
        try
        {
            tagRecords = await Intake.ListLinkableRecordsAsync(ProjectId, tagRecordType);
        }
        catch
        {
            tagRecords = Array.Empty<LinkableRecord>();
            tagError = $"Couldn't load this project's {RecordTypeLabelPlural(tagRecordType)}. Please try again.";
        }
        finally
        {
            tagRecordsLoading = false;
            StateHasChanged();
        }
    }

    // Already carried by the email — offered greyed so the picker says why rather than failing.
    private static bool AlreadyTagged(ProjectCommunication item, LinkableRecord record) =>
        item.Message.Categories.Any(category =>
            string.Equals(category, "JPMS/" + record.TagReference, StringComparison.OrdinalIgnoreCase));

    private async Task AddTagAsync(ProjectCommunication item)
    {
        if (tagBusy || string.IsNullOrWhiteSpace(tagRecordId)) return;
        var record = tagRecords.FirstOrDefault(r => r.RecordId == tagRecordId);
        if (record is null) return;
        tagError = null;
        tagBusy = true;
        try
        {
            // The type to link as is the picked RECORD's own type, not the dropdown's (the
            // Scheduling picker lists NOD/EOT/LAD claims documents alongside the bucket).
            // Cross-pathway links file the thread under both, exactly as the Control Centre does.
            await Intake.LinkMessageToRecordAsync(
                item.Message.Id, item.Message.InternetMessageId, record.Type, record.RecordId,
                allowCrossPathway: true);
            tagAdded = (item.Message.Id, $"Tagged to {record.Reference} — {record.Title}. It now shows under that record too.");
            tagPickerMessageId = null;
            tagRecordId = "";
            // The tag lives in the mailbox; re-read so the new chip (and any pathway change)
            // shows straight away. The search or page in view is re-run as-is.
            await LoadAsync(reset: true);
        }
        catch (CommandFailedException ex)
        {
            tagError = ex.Message;
        }
        catch
        {
            tagError = "That tag didn't apply. Please try again.";
        }
        finally
        {
            tagBusy = false;
        }
    }

    private async Task LoadAsync(bool reset)
    {
        if (reset)
        {
            loaded = false;
            items.Clear();
            nextCursor = null;
            total = 0;
        }
        loadError = null;
        loadingMore = !reset;
        try
        {
            var page = await Queries.AskAsync(
                new ListProjectCommunications(ProjectId, TypeFilter, nextCursor, PageSize, bucketFilter, activeSearch),
                CancellationToken.None);
            items.AddRange(page.Items);
            nextCursor = page.NextCursor;
            total = page.Total;
        }
        catch
        {
            loadError = "Couldn't load the project's communications. Please try again.";
        }
        finally
        {
            loaded = true;
            loadingMore = false;
        }
    }

    private string EmptyHeadline =>
        activeSearch is { } query
            ? $"No tagged emails match “{query}”."
            : TypeFilter is { } type
            ? $"No emails tagged to {TypeOptions.First(o => o.Type == type).Label} yet."
            : bucketFilter is { } bucket
                ? $"No tagged emails on the {bucket} pathway yet."
                : "No tagged emails yet.";

    // Short pathway label from the thread's bucket category ("JPMS/Client" → "Client"); null when
    // the thread has no pathway yet. Clients read Message.Bucket, never parse tag strings — this
    // only strips the mailbox prefix for display.
    private static string? BucketLabel(string? bucket) =>
        string.IsNullOrEmpty(bucket) ? null
        : bucket.StartsWith("JPMS/", StringComparison.OrdinalIgnoreCase) ? bucket["JPMS/".Length..]
        : bucket;

    private static string BucketChipClass(string pathway)
    {
        const string baseClass = "inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-medium shrink-0 ";
        return pathway switch
        {
            "Client"        => baseClass + "bg-accent/10 text-accent",
            "Subcontractor" => baseClass + "bg-positive/10 text-positive",
            "Supplier"      => baseClass + "bg-sky-500/10 text-sky-600",
            _               => baseClass + "bg-surface-raised border border-line text-content-muted"
        };
    }

    // Chip label per record type — singular, canonical UI terms.
    private static string TypeLabel(RecordType type) => type switch
    {
        RecordType.Request          => "Request",
        RecordType.BidPackageInvite => "Bid package",
        RecordType.WorkOrder        => "Work order",
        RecordType.CostCentre       => "Cost centre",
        RecordType.Scheduling       => "Relevant Event",
        RecordType.Todo             => "To-do",
        RecordType.CalendarEvent    => "Calendar event",
        RecordType.Lad              => "LADs",
        RecordType.Variation        => "VO",
        RecordType.VariationQuote   => "Variation",
        RecordType.Defect           => "Defect",
        RecordType.Inventory        => "Inventory item",
        RecordType.SiteInstruction  => "Site instruction",
        RecordType.ValuationClaim   => "Valuation claim",
        RecordType.ValuationReportSnapshot => "Valuation snapshot",
        RecordType.SubcontractorComms => "Subcontractor comms",
        RecordType.SupplierComms    => "Supplier comms",
        RecordType.InternalComms    => "Internal comms",
        _                           => type.ToString()
    };

    private static string DisplayFrom(MailboxMessage message) =>
        string.IsNullOrWhiteSpace(message.FromName) ? message.FromEmail : message.FromName;

    // The email's workflow tags that didn't resolve to one of this project's records (the resolved
    // ones already render as record chips). The bare "JPMS" marker never reaches the client.
    private static IEnumerable<string> UnresolvedTags(ProjectCommunication item) =>
        item.Message.Categories.Where(category =>
            !item.Links.Any(link => string.Equals(link.Tag, category, StringComparison.OrdinalIgnoreCase)));

    private static string TagStem(string tag) =>
        tag.StartsWith("JPMS/", StringComparison.OrdinalIgnoreCase) ? tag["JPMS/".Length..] : tag;
}
