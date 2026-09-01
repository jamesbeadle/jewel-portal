using Jewel.JPMS.Features.Triage;

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
                new ListProjectCommunications(ProjectId, TypeFilter, nextCursor, PageSize, bucketFilter),
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
        TypeFilter is { } type
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
