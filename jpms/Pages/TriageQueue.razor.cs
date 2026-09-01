using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // The communication pathway — WHO the correspondence is with (docs/Pathway-Split-Platform-Flow-
    // Plan.md §2). Triage is pathway-FIRST: the pathway is chosen (or already fixed on the thread)
    // before any action, and it decides which action tabs and record types are offered.
    private enum TriagePathway { Client, Subcontractor, Supplier, Internal }
    private static readonly TriagePathway[] PathwayOptions =
    {
        TriagePathway.Client, TriagePathway.Subcontractor, TriagePathway.Supplier, TriagePathway.Internal
    };

    // The triager's pathway choice for the SELECTED email. Pre-set from the thread's own bucket when
    // it already carries one (then shown as a fixed badge — the routing decision was made when the
    // thread was first filed); otherwise null until chosen, and only Discard is offered until it is
    // (discarding is pathway-less).
    private TriagePathway? pathway;

    // The pathway bucket categories exactly as the server stamps them — MailboxMessage.Bucket carries
    // one of these verbatim. Literals here because the WASM app references contracts only; the API's
    // TriageCategories constants aren't visible to it.
    private const string ClientBucket = "JPMS/Client";
    private const string SubcontractorBucket = "JPMS/Subcontractor";
    private const string SupplierBucket = "JPMS/Supplier";
    private const string InternalBucket = "JPMS/Internal";

    private static TriagePathway? PathwayFromBucket(string? bucket)
    {
        if (string.IsNullOrEmpty(bucket)) return null;
        if (bucket.Equals(ClientBucket, StringComparison.OrdinalIgnoreCase)) return TriagePathway.Client;
        if (bucket.Equals(SubcontractorBucket, StringComparison.OrdinalIgnoreCase)) return TriagePathway.Subcontractor;
        if (bucket.Equals(SupplierBucket, StringComparison.OrdinalIgnoreCase)) return TriagePathway.Supplier;
        if (bucket.Equals(InternalBucket, StringComparison.OrdinalIgnoreCase)) return TriagePathway.Internal;
        return null;
    }

    // The selected thread's already-decided pathway (null = not filed yet). When set, the selector
    // renders as a fixed "Filed under …" badge and the pathway can't be switched — the triager still
    // acts within it.
    private TriagePathway? FixedPathway => PathwayFromBucket(selected?.Bucket);

    // Enum names double as the user-facing labels AND the short pathway strings the server's
    // commands accept ("Client" / "Subcontractor" / "Internal") — keep them in sync with MapPathway.
    private static string PathwayLabel(TriagePathway p) => p.ToString();

    // Pathway chip colours: green-ish Client, orange-ish Subcontractor, blue-ish Supplier,
    // purple-ish Internal — the same rounded-pill shape as the tag chips, but distinct hues so
    // rows can be scanned by pathway.
    private static string PathwayChipColour(TriagePathway p) => p switch
    {
        TriagePathway.Client        => "bg-emerald-500/10 text-emerald-600",
        TriagePathway.Subcontractor => "bg-orange-500/10 text-orange-600",
        TriagePathway.Supplier      => "bg-sky-500/10 text-sky-600",
        TriagePathway.Internal      => "bg-purple-500/10 text-purple-600",
        _                           => "bg-accent/10 text-accent"
    };

    // The page shows the live queue (untagged Inbox), the discarded pile, or every tagged email (the
    // management surface for adding/removing workflow tags). The detail pane is shared.
    private enum QueueView { Active, Discarded, Tagged }
    private QueueView view = QueueView.Active;

    // Every record type an email can be LINKED to. Driven by the providers registered server-side;
    // adding a type here surfaces it once its provider exists. Under the pathway-first UI this full
    // list is only the fallback for a tagged thread that has no pathway yet — the pathway-filtered
    // subsets below are what the pickers normally offer.
    // Cost Centre was removed as a link target 2026-08-04 (it never earned its place as a filing
    // destination; existing CC-tagged mail keeps reading fine). VariationQuote is folded into
    // Variation — one record, one number, per the 2026-07-23 unification.
    private static readonly RecordType[] RecordTypeOptions =
    {
        RecordType.Request, RecordType.BidPackageInvite, RecordType.WorkOrder, RecordType.Scheduling, RecordType.Lad, RecordType.Variation, RecordType.Todo, RecordType.CalendarEvent, RecordType.BuildingControlInspection, RecordType.BuildingControlCase, RecordType.Inventory
    };

    // What each pathway's "Link to existing" offers (the pathway filters the actions — the plan's
    // §2.2): client-side records only under Client, subcontract-side only under Subcontractor, and
    // Internal links to existing to-do items. CostCentre appears on BOTH sides of the wall because
    // cost-centre mail can be valuation-side or subcontract-side — the pathway choice decides, and
    // travels with the link command.
    private static readonly RecordType[] ClientLinkTypes =
    {
        RecordType.Request, RecordType.Variation, RecordType.Lad, RecordType.Scheduling,
        RecordType.BuildingControlInspection, RecordType.BuildingControlCase
    };
    // Subcontractor covers the whole subcontract lifecycle, not just the tender: an email can land
    // before a package exists (a chase, an H&S request), against the order that followed the award
    // (WorkOrder — the plan's §2.2 "link work order"), or onto a to-do that tracks something the
    // supplier owes us. Todo is offered here because to-do links are pathway-neutral, so tagging one
    // never re-files the thread — the same reason the Tagged picker offers it on every pathway.
    private static readonly RecordType[] SubcontractorLinkTypes =
    {
        RecordType.BidPackageInvite, RecordType.WorkOrder
    };
    private static readonly RecordType[] InternalLinkTypes = { RecordType.Todo, RecordType.CalendarEvent };
    // Inventory (2026-08-28) is the Supplier pathway's first linkable record type — goods for the
    // job, raised from a supplier email or on the project's Inventory tab.
    private static readonly RecordType[] SupplierLinkTypes = { RecordType.Inventory };

    // The Tagged tab's "link to another record" pool: since the hard client wall was removed
    // (2026-08-21) every type is offered whatever the thread's pathway. Each option's label shows
    // the pathway it files under, and a link that would file the thread under a second pathway
    // simply files it under both (the confirm step was retired 2026-08-28).

    private IReadOnlyList<RecordType> QueueLinkTypeOptions => pathway switch
    {
        TriagePathway.Client        => ClientLinkTypes,
        TriagePathway.Subcontractor => SubcontractorLinkTypes,
        TriagePathway.Supplier      => SupplierLinkTypes,
        TriagePathway.Internal      => InternalLinkTypes,
        _                           => Array.Empty<RecordType>()
    };

    private IReadOnlyList<RecordType> TaggedLinkTypeOptions => RecordTypeOptions;

    // The pathway a record type files a thread under, as a TriagePathway — mirrors the server's
    // TriageCategories.BucketFor. Null = pathway-neutral (Todo) or per-email choice (CostCentre).
    // Drives the Tagged picker's cross-filing heads-up.
    private static TriagePathway? ImpliedPathway(RecordType type) => type switch
    {
        RecordType.Request or RecordType.Variation or RecordType.VariationQuote
            or RecordType.Scheduling or RecordType.Lad or RecordType.TenderEnquiry => TriagePathway.Client,
        RecordType.BidPackageInvite or RecordType.WorkOrder
            or RecordType.SubcontractorComms => TriagePathway.Subcontractor,
        RecordType.SupplierComms or RecordType.Inventory => TriagePathway.Supplier,
        RecordType.InternalComms => TriagePathway.Internal,
        _ => null
    };

    // The pathway a record type files a thread under, for the Tagged picker's labels — mirrors the
    // server's TriageCategories.BucketFor so the triager sees where a link would put the thread.
    private static string RecordTypePathwayLabel(RecordType type) => type switch
    {
        RecordType.Request or RecordType.Variation or RecordType.VariationQuote
            or RecordType.Scheduling or RecordType.Lad or RecordType.TenderEnquiry
            or RecordType.BuildingControlCase or RecordType.BuildingControlInspection => "Client",
        RecordType.BidPackageInvite or RecordType.WorkOrder
            or RecordType.SubcontractorComms => "Subcontractor",
        RecordType.SupplierComms    => "Supplier",
        RecordType.Inventory        => "Supplier",
        RecordType.InternalComms    => "Internal",
        RecordType.CostCentre       => "Client or Subcontractor",
        RecordType.Todo             => "Neutral",
        RecordType.CalendarEvent    => "Neutral",
        _ => ""
    };

    // The optional "link these to-dos to an open request" picker, offered on every pathway EXCEPT
    // Subcontractor. A request is a Client record: tagging one files the thread under Client, which is
    // right for an internal thread that turns out to be client business. (The hard client wall was
    // removed 2026-08-21 — the hiding is kept so the Subcontractor → To-dos action stays neutral by
    // default; subcontract mail that is really client business files via the Tagged picker's confirm.)

    private static string RecordTypeLabel(RecordType type) => type switch
    {
        RecordType.Request          => "Request",
        RecordType.BidPackageInvite => "Bid Package Invite",
        RecordType.WorkOrder        => "Work Order",
        RecordType.CostCentre       => "Cost Centre",
        // UI terminology is "Relevant Event" (what the programme bucket holds — decision
        // 2026-08-07); the RecordType/tag layer keeps its Scheduling identifiers.
        RecordType.Scheduling       => "Relevant Event",
        RecordType.Variation        => "Variation Order",
        RecordType.VariationQuote   => "Variation Order Quote",
        RecordType.Lad              => "LADs claim",
        RecordType.Todo             => "To-do item",
        RecordType.CalendarEvent    => "Calendar event",
        RecordType.BuildingControlInspection => "Building Control Inspection",
        RecordType.BuildingControlCase => "Building Control Case",
        RecordType.SubcontractorComms => "Subcontractor communication",
        RecordType.SupplierComms    => "Supplier communication",
        RecordType.InternalComms    => "Internal communication",
        RecordType.Inventory        => "Inventory item",
        _                           => type.ToString()
    };

    // Lower-case plural for the generic "Loading …" / "No … on this project" copy. Scheduling lists
    // the project's bucket plus its claims documents (NOD/EOT/LADs), so its plural reads as that set.
    private static string RecordTypeLabelPlural(RecordType type) => type switch
    {
        RecordType.Scheduling => "relevant events and claims documents",
        RecordType.Variation      => "variation orders",
        RecordType.VariationQuote => "variation order quotes",
        RecordType.Lad        => "LADs claims",
        RecordType.Todo       => "to-do items",
        _                     => $"{RecordTypeLabel(type).ToLowerInvariant()}s"
    };

    private static string RecordTypeLabelSingular(RecordType type) => type switch
    {
        RecordType.Scheduling => "relevant event or claims document",
        RecordType.Variation      => "variation order",
        RecordType.VariationQuote => "variation order quote",
        RecordType.Lad        => "LADs claim",
        RecordType.Todo       => "to-do item",
        _                     => RecordTypeLabel(type).ToLowerInvariant()
    };

    private const int PageSize = 5;

    // Session checked and the user signed in. This is NOT "the mailbox is here" — keeping the two
    // apart is what lets the heaviest page in the app show its chrome while four fetches are still
    // out, without ever claiming a count it does not have.
    private bool sessionReady;
    private string? loadError;
    // True while a list page (queue / discarded / tagged) is being re-fetched — drives the inline
    // spinner over the list column so pagination and filter changes fade rather than jolt.
    private bool listLoading;

    // Has this list ANSWERED yet — a failed answer counts, or the jewel pulses forever. listLoading
    // can't stand in: it is false before the first fetch starts as well as after it finishes, and a
    // total of 0 read in that gap is the "nothing in the queue" this page must never show early.
    private bool queueArrived;
    private bool discardedArrived;
    private bool taggedArrived;
    private bool unassignedArrived;

    // The current page only. Graph pages with an opaque cursor, so we keep a small stack of the
    // cursors we've walked (index 0 = first page, cursor null) to support Previous/Next.
    private IReadOnlyList<MailboxMessage> items = Array.Empty<MailboxMessage>();
    private int total;
    private List<string?> queueCursors = new() { null };
    private int queueIndex;
    private string? queueNext;

    private IReadOnlyList<MailboxMessage> discardedItems = Array.Empty<MailboxMessage>();
    private int discardedTotal;
    private List<string?> discardedCursors = new() { null };
    private int discardedIndex;
    private string? discardedNext;

    private IReadOnlyList<MailboxMessage> taggedItems = Array.Empty<MailboxMessage>();
    private int taggedTotal;
    private List<string?> taggedCursors = new() { null };
    private int taggedIndex;
    private string? taggedNext;
    // The Tagged tab's multi-select filter: the set of tags currently ticked (empty = every tagged
    // email), whether the dropdown is open, and the set of tags we've seen (offered in the dropdown).
    // "Discarded" is always offered; the rest accrue as tagged emails load.
    private readonly HashSet<string> selectedTags = new(StringComparer.OrdinalIgnoreCase);
    private bool filterOpen;
    private readonly SortedSet<string> knownTags = new(StringComparer.OrdinalIgnoreCase) { "JPMS/Discarded", "JPMS/Replied" };

    // The Tagged tab's search box (see the markup comment). A resolved reference lives in
    // taggedSearchRecord + taggedSearchTag (the "JPMS/…" category LoadTaggedAsync filters by,
    // taking the one server-side filter slot); free-text results live in taggedSearchResults
    // (null = not in free-text mode) and render in the list's place, unpaged.
    private string taggedSearch = "";
    private string taggedSearchPending = "";
    private bool taggedSearching;
    private LinkableRecord? taggedSearchRecord;
    private string? taggedSearchTag;
    private IReadOnlyList<MailboxMessage>? taggedSearchResults;
    private CancellationTokenSource? taggedSearchDebounce;

    private bool TaggedSearchActive =>
        taggedSearching || taggedSearchTag is not null || taggedSearchResults is not null;

    private string? PathwayDisabledTitle =>
        TaggedSearchActive ? "The search owns the filter — clear it to use these" : null;

    // The Tagged tab's pathway filter: one of the three bucket tags, or null = all pathways. Sent to
    // the server through the same tags parameter as the record-tag filter (the bucket IS a category,
    // and ListByTagsAsync filters by exact category). Mutually exclusive with the record-tag
    // multi-select: Graph only composes category filters with OR, so combining the two would read as
    // a union rather than the intersection a user would expect — picking one clears the other.
    private string? pathwayBucketFilter;

    // Cross-filing (plan §2.3): a pathway crossing — Client↔Subcontractor/Internal included —
    // files the thread under both sides, no confirm asked (retired 2026-08-28: the pathway panes
    // make the second filing an explicit, visible choice, so the old "Confirm the cross-filing" /
    // "File under both anyway" round-trip was a second ask for a decision already made on
    // screen). Every link command is sent with AllowCrossPathway: true, which also keeps an
    // older api from prompting.

    // Records ALREADY raised from the selected email this session — System Actions' Create now
    // (and Apply's create) add a chip here so the pane says what exists, with its reference,
    // rather than the staged "will raise" wording. Parked/restored per email like the staging.
    private readonly List<CreatedNowRecord> createdNowRecords = new();

    // Sort order for all three lists: oldest-first (the default — clear the backlog from page one)
    // or newest-first. The last choice is remembered per user, like the work-order grouping toggle.
    private bool newestFirst;

    private MailboxMessage? selected;
    private MailboxMessageDetail? detail;
    private bool detailLoading;
    private CancellationTokenSource? detailCts;

    // The selected email's whole conversation (oldest first, tags and all), for the thread panel.
    // Guarded at render time by the current selection's ConversationId, so a stale list from a
    // previous selection can never show against the wrong email.
    private IReadOnlyList<MailboxMessage> thread = Array.Empty<MailboxMessage>();
    private bool threadLoading;
    // The thread came from a subject match because Outlook's conversation id had split from the
    // rest of the chain — shown as such, since Apply's thread-wide sweep follows the id, not the subject.
    private bool threadMatchedBySubject;
    // The thread read failed outright (rather than finding nothing) — said so, never hidden
    // (Nigel, 2026-08-22: an empty panel read as "no thread" when the read had simply failed).
    private string? threadError;
    private CancellationTokenSource? threadCts;

    private IReadOnlyList<Request> unassigned = Array.Empty<Request>();
    private string? unassignedError;

    // The audit register's latest triage decisions, for the "Recently processed" section beneath
    // the workspace. Nullable backing field: null is the honest "not fetched yet" (the section
    // simply doesn't render), an empty list is a real answer that keeps it hidden either way.
    private IReadOnlyList<AuditEvent>? recentTriage;
    private IReadOnlyList<AuditEvent> RecentTriage => recentTriage ?? Array.Empty<AuditEvent>();
    private const int RecentTriageCount = 8;

    // Which register rows count as "triaged into a document": an email linked to an existing record,
    // or a record created from one. EmailTriaged/DraftCreated rows describe the same actions from
    // another angle, so listing them too would double every entry.
    private static bool IsRecentTriageEvent(AuditEvent entry) =>
        entry.EventType is AuditEventType.RecordLinked or AuditEventType.RecordCreatedFromEmail;

    // Refreshed alongside the queue after every consuming action, so the row for the email just
    // triaged appears as its selection clears. A failed read leaves the panel absent rather than
    // blocking triage — the query client has already reported the failure to the error toast.
    private async Task LoadRecentTriageAsync()
    {
        try
        {
            var page = await Queries.AskAsync(new ListAuditEvents(Take: 25), CancellationToken.None);
            recentTriage = page.Items.Where(IsRecentTriageEvent).Take(RecentTriageCount).ToList();
        }
        catch { /* reported by the query client; the panel stays absent */ }
    }

    // Where each record family's page lives — the click-through from a recently-triaged row to the
    // document the email was filed against. Families without a per-record page land on the closest
    // project tab (work orders, programme, financials, to-dos); anything unresolvable reads as text.
    private static string? RecentHref(AuditEvent entry)
    {
        if (entry.RecordType == RecordType.SubcontractorComms) return "/subcontractors/communications";
        if (entry.RecordType == RecordType.SupplierComms) return "/suppliers/communications";
        if (entry.RecordType == RecordType.InternalComms) return "/internal/communications";
        if (string.IsNullOrEmpty(entry.ProjectId))
            return entry.RecordType == RecordType.Todo ? "/todos" : null;
        var projectId = entry.ProjectId;
        var recordId = entry.RecordId;
        return entry.RecordType switch
        {
            RecordType.Request => string.IsNullOrEmpty(recordId) ? null : $"/projects/{projectId}/requests/view/{recordId}",
            RecordType.Variation or RecordType.VariationQuote => string.IsNullOrEmpty(recordId) ? null : $"/projects/{projectId}/variations/{recordId}",
            RecordType.BidPackageInvite => string.IsNullOrEmpty(recordId) ? null : $"/projects/{projectId}/bid-package-invites/{recordId}",
            RecordType.WorkOrder  => $"/projects/{projectId}/work-orders",
            RecordType.Scheduling or RecordType.Lad => $"/projects/{projectId}/programme",
            RecordType.CostCentre => $"/projects/{projectId}/financials",
            RecordType.Todo       => $"/projects/{projectId}/todos",
            RecordType.Inventory  => $"/projects/{projectId}/inventory",
            _ => null
        };
    }

    // The right-hand meta on a recently-triaged row: project reference (when the project is known)
    // and who did it. Reads AllProjects so a completed project's reference still resolves.
    private string RecentMeta(AuditEvent entry)
    {
        var project = AllProjects.FirstOrDefault(p => p.ProjectId == entry.ProjectId);
        return project is null ? entry.ActorEmail : $"{project.Reference} · {entry.ActorEmail}";
    }

    // Compact relative stamp for scanning (mirrors the audit trail page); the exact moment sits in
    // the row's hover title.
    private static string Ago(DateTimeOffset at)
    {
        var span = DateTimeOffset.UtcNow - at;
        if (span < TimeSpan.FromMinutes(1)) return "just now";
        if (span < TimeSpan.FromHours(1)) return $"{(int)span.TotalMinutes}m ago";
        if (span < TimeSpan.FromHours(24)) return $"{(int)span.TotalHours}h ago";
        if (span < TimeSpan.FromDays(30)) return $"{(int)span.TotalDays}d ago";
        return at.LocalDateTime.ToString("d MMM yyyy");
    }

    // Triage is restricted to administrators, project managers, and the finance director.
    // Administrators are granted every role server-side, so they always carry Role.ProjectManager too.
    private bool CanTriage =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ProjectManager or Role.FinanceDirector);

    // Total pages for each list, derived from the server-reported total count.
    private int QueuePageCount => Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
    private int DiscardedPageCount => Math.Max(1, (int)Math.Ceiling(discardedTotal / (double)PageSize));
    private int TaggedPageCount => Math.Max(1, (int)Math.Ceiling(taggedTotal / (double)PageSize));

    private bool busy;
    // Caption under the detail-pane spinner while an action is in flight, e.g. "Discarding".
    private string busyLabel = "Working";
    private string? actionError;

    // Link state. The SYSTEM TAGS MODAL is where queue-side picking happens now; these fields back
    // the TAGGED tab's single-pick "add another tag" dropdown (linkRecordType + linkRecordId +
    // linkRecords pool), while pickedRecords holds the modal's staged picks for the queue apply.
    private RecordType linkRecordType = RecordType.Request;
    private string linkRecordId = "";
    // The staged record links — whole LinkableRecords (not ids), so picks survive switching the
    // record-type filter and one email can be linked to, say, a variation AND an RFI in one apply.
    private readonly List<LinkableRecord> pickedRecords = new();
    // Set when a multi-pick link partially failed: the email left the queue (first tag stuck) but
    // some picks didn't take, so the notice — shown in place of the cleared selection — says which
    // and points at the Tagged view to finish. Dismissable; cleared on the next selection/link.
    private string? linkNote;
    // What happened to the automatic purchase-order email after a work order was raised
    // un-drafted from this queue — sent, saved as a mailbox draft, or skipped and why. Shown in
    // place of the cleared selection like linkNote; dismissable; cleared on the next selection.
    private string? poEmailNote;
    private bool poEmailNoteIsSuccess;
    private IReadOnlyList<LinkableRecord> linkRecords = Array.Empty<LinkableRecord>();
    private bool linkRecordsLoading;

    // Create-new state lives in `stagedCreate` (Features.Triage.StagedRecordCreate), drafted in
    // the System Actions pane (moved from System Tags 2026-08-10). Its RequestKind says whether
    // the Request kind raises a General container (REQ-####) or an official RFI.

    // Reply-in-thread state: the chosen project, the reply written in the portal (it becomes the
    // draft's body AND the background request's description), and the last successful outcome
    // (created request + staged draft). The outcome outlives the action — RunAction clears the
    // selection because the email left the queue — so the banner with the draft's Outlook link
    // renders in the empty detail pane until the triager selects another email or switches view.
    private string replyBody = "";
    // The composer's envelope — semicolon/comma-separated address inputs, prefilled reply-all from
    // the opened email's detail and freely editable. What these show is exactly what is sent.
    private string replyToField = "";
    private string replyCcField = "";
    private string replyBccField = "";
    private string replySubject = "";
    private bool replyShowBcc;
    // The reply composer is its own section under the filing tabs — open on demand, combinable
    // with whatever filing is set up above (one Send applies both).
    private bool replyOpen;
    // The open composer is a FORWARD of the open email rather than a reply: blank envelope, "FW:"
    // subject, original attachments carried automatically by Graph, and the send never counts as
    // a triage decision (the email stays queued unless something else files it).
    private bool replyIsForward;
    // Envelope ownership, once per selection: set when the detail's reply-all prefill lands OR
    // the moment the user edits any envelope field (2026-08-28) — whichever comes first wins, so
    // an address typed before the slow fetch arrives is never overwritten by it.
    private bool replyEnvelopePrefilled;
    private IReadOnlyList<ComposeDraftAttachment> replyAttachments = Array.Empty<ComposeDraftAttachment>();
    private ComposeOutcome? composeOutcome;

    // ---- New email (fresh outbound thread) ----
    private bool newEmailBusy;
    private string? newEmailError;
    private string newEmailTo = "";
    private string newEmailCc = "";
    private string newEmailBcc = "";
    private string newEmailSubject = "";
    private string newEmailBody = "";
    private IReadOnlyList<ComposeDraftAttachment> newEmailAttachments = Array.Empty<ComposeDraftAttachment>();
    private bool newEmailFile;
    private string newEmailProjectId = "";
    private RecordType newEmailRecordType = RecordType.Request;
    private string newEmailRecordId = "";
    private bool newEmailRecordsLoading;
    private IReadOnlyList<LinkableRecord> newEmailRecords = Array.Empty<LinkableRecord>();

    // To-do draft rows (Features.Triage.TodoDraftRow), edited in the System Actions pane's
    // "Create To-do Items" action; one to-do is raised PER ASSIGNEE per row when the apply runs.
    private List<TodoDraftRow> createTodoRows = new() { new TodoDraftRow() };

    // Section 2's own project pick (independent of section 3's triageProjectId — both sections are
    // on screen at once now, so they can no longer share state). Blank = company-wide items.
    // THE project for this email — a global, first-class triage step (Nigel, 2026-08-04 v5): set
    // once (auto-matched from the email text where possible) and used by everything — the to-dos,
    // the record pickers, the attachment tray. Blank = no project (company-wide to-dos only).
    private string triageProjectId = "";
    // True when the project was matched automatically from the email/thread text, so the UI can
    // say so ("matched from the email — change it if that's wrong").
    private bool projectAutoMatched;

    // ---- The workspace: which content each of the two windows shows, the divider position, and
    //      the open preview document. Everything pane-shaped goes through this one object; the
    //      page re-renders on its OnChange so a rail press anywhere redraws both windows. ----
    private readonly PanelWorkspaceState workspace = new();

    // A document opened from inside a record (the record explorer's Preview buttons) — shown in
    // the Preview pane on the window OPPOSITE the explorer, so record and document read together.
    private void OpenRecordPreview(PreviewRequest document) =>
        workspace.OpenPreview(document, PanelKind.Records);

    // A document opened from a Xero transaction — same Preview pane, opposite the explorer.
    private void OpenXeroPreview(PreviewRequest document) =>
        workspace.OpenPreview(document, PanelKind.Xero);

    // An attachment opened from the subcontractor communications browser — same Preview pane,
    // opposite the browser, so the email thread and its document read together.
    private void OpenPathwayPreview(PreviewRequest document, PanelKind anchor) =>
        workspace.OpenPreview(document, anchor);

    // System actions lined up in the Actions pane, run by the one Apply after the filing —
    // payloads snapshotted at stage time, cleared per selection like all staging.
    private readonly List<StagedSystemAction> stagedSystemActions = new();

    // ---- The Outbox: replies and forwards lined up against OLDER emails (started from a Reply
    //      or Forward button on a record's correspondence or the subcontractor comms browser),
    //      sent by the one Apply.
    //      Deliberately WORKSPACE-LEVEL, not per-selection staging: they survive moving between
    //      inbox emails, and each anchor email is tagged with whatever System Tags picks are
    //      staged when the apply actually runs (decision 2026-08-12) — one triage decision
    //      covering the open email and every email being answered. ----
    private readonly List<StagedOutboxReply> queuedReplies = new();
    // The older email a Reply or Forward press just chose — the Outbox pane opens its composer
    // for it (outboxComposeAnchorIsForward says which button it was). Cleared (by the pane) when
    // the entry is lined up or the composer discarded.
    private MailboxMessage? outboxComposeAnchor;
    private bool outboxComposeAnchorIsForward;
    // The Outbox badge counts everything Apply will send: lined-up replies + the open email's own.
    private int OutboxSendCount => queuedReplies.Count + (ReplyDraftPending ? 1 : 0);
    // What the last apply sent from the Outbox — shown with the other outcome banners where the
    // cleared selection was; dismissable; cleared on the next selection.
    private string? outboxNote;

    // A Reply (or Forward) pressed on an older email anywhere in the workspace: composing happens
    // in the Outbox pane, opened OPPOSITE the list it came from (like a preview) so thread and
    // reply read side by side — the flow is identical from every entry point.
    private void StartOutboxReply(MailboxMessage message, PanelKind anchor)
    {
        outboxComposeAnchor = message;
        outboxComposeAnchorIsForward = false;
        workspace.ShowOpposite(PanelKind.Outbox, anchor);
    }

    private void StartOutboxForward(MailboxMessage message, PanelKind anchor)
    {
        outboxComposeAnchor = message;
        outboxComposeAnchorIsForward = true;
        outboxForwardTo = null;
        workspace.ShowOpposite(PanelKind.Outbox, anchor);
    }

    // Recipients a forward opens with — set by "Forward to QS", cleared by every other forward.
    private string? outboxForwardTo;

    private void StartForwardToQs()
    {
        if (selected is null) return;
        StartOutboxForward(selected, PanelKind.Client);
        outboxForwardTo = string.Join("; ", QsRecipients.Select(person => person.Email));
    }

    // "Edit in Email window" on the Outbox's current-reply row — that composer lives under the
    // open email, so open its section and show the Email window beside the Outbox.
    private void ShowCurrentReplyComposer()
    {
        replyOpen = true;
        workspace.ShowOpposite(PanelKind.Email, PanelKind.Outbox);
    }

    // The staged work + armed discard. The System Tags pane's tab mirrors the page's own
    // `pathway` field (the pathway decision); stagedCreate is the pane's drafted new record
    // (null = none) — StagedRecordKind decides whether Apply raises a request, a bid package, a work order or a defect.
    private bool discardArmed;
    private StagedRecordCreate? stagedCreate;
    // The "Relevant Event for Programme" decision — staged like everything else, applied by the
    // one Apply. Lives OUTSIDE System Tags because the programme bucket isn't a record anyone
    // picks or creates: every project has exactly one, so filing to it is a yes/no, not a search.
    // Nullable on purpose: null = not yet answered (the Yes/No pair renders blank), and Apply
    // refuses to run until the triager picks a side — a conscious decision, never a default.
    private bool? relevantEventStaged;
    // The "Entire thread" decision: Yes means every action in the apply spreads across the whole
    // current conversation (LinkThreadScope.EntireThread); No means each action tags only the
    // clicked email (MessageOnly). Nullable like the Relevant Event decision above — blank until
    // answered, required before Apply. Never persisted, cleared back to blank with the rest of
    // the staging on every selection/view change and after every apply.
    private bool? triageEntireThread;
    // The "Use existing tags" decision, offered only when the open email's thread ALREADY carries
    // record tags (the queue row's outline "Thread:" chips). Yes means Apply files this email
    // under those same records — the stems resolve back to records (ResolveRecordTags, the same
    // resolver behind the search chips) and each links exactly like a picked record — so a reply
    // to an already-linked thread is triaged in one answer, with nothing new to pick. No means
    // the triager picks this email's records themselves. Nullable like the two decisions above —
    // blank until answered, required before Apply whenever the row is on show.
    private bool? useThreadTags;

    // What the Subcontractor Communications browser tags against: the open QUEUE email (the
    // Tagged view manages its tags from the email pane instead), and the triage bar's project —
    // by name, because record-less communication tags carry no project to filter on.
    private string OpenQueueEmailSubject =>
        view == QueueView.Active && selected is not null ? selected.Subject : "";

    private string TriageProjectName =>
        AllProjects.FirstOrDefault(project => project.ProjectId == triageProjectId)?.Name ?? "";

    // Staging from a pathway pane IS the pathway decision (as the old System Tags tab switch
    // was) — parse the pane's label back onto the page's own pathway state so filing, to-dos and
    // a record-less reply all read one field.
    private void OnPathwayEngaged(string paneLabel)
    {
        if (Enum.TryParse<TriagePathway>(paneLabel, out var next)) SetPathway(next);
    }


    // Each pathway icon's badge = the staged work that pane owns: its record picks and category
    // ticks, its own staged actions, the drafted new record and the drafted to-dos. Every action
    // kind lives on exactly one pane (no shared "General" group — 2026-08-27 review), so the
    // kind→pane map is the pane configs themselves.
    private int PathwayBadge(PathwayPaneConfig config) =>
        pickedRecords.Count(record => config.LinkTypes.Contains(record.Type)
            || (config.Family is { } family && family.All.Any(familyRecord => familyRecord.RecordId == record.RecordId)))
        // Kinds can be offered on more than one pane (directory contact: Subcontractor,
        // Supplier, Internal), so staged actions count where they were STAGED, not by kind.
        + stagedSystemActions.Count(action => action.Pathway is { } stagedFrom
            ? stagedFrom == config.Pathway
            : config.AllActionKinds.Contains(action.Kind))
        + (config.Pathway == "Internal" ? CurrentTodoDrafts().Count : 0)
        + (StagedCreateReady && StagedCreatePathway(stagedCreate!.Kind) == config.Pathway ? 1 : 0);

    // Which pane's badge a drafted record counts on — mirrors which pane offers its create.
    private static string? StagedCreatePathway(StagedRecordKind kind) => kind switch
    {
        StagedRecordKind.Request or StagedRecordKind.TenderEnquiry
            or StagedRecordKind.BuildingControlInspection => "Client",
        StagedRecordKind.BidPackage or StagedRecordKind.WorkOrder or StagedRecordKind.Defect => "Subcontractor",
        StagedRecordKind.Inventory => "Supplier",
        StagedRecordKind.CalendarEvent => "Internal", // raised from the Internal pane, beside the Calendar
        _ => null
    };

    private bool StagedCreateReady => stagedCreate is { } sc && sc.IsReady;

    // A tender enquiry usually brings its own Lead project, so it needs no project in the bar.
    private bool StagedCreatesOwnProject =>
        stagedCreate is { Kind: StagedRecordKind.TenderEnquiry } sc && sc.TenderEnquiry.CreatesNewProject;

    // Joining an existing project is only ever the same job's second email — the bar's project
    // must itself still be a Lead.
    private bool TriageProjectIsLead =>
        !string.IsNullOrWhiteSpace(triageProjectId) && Projects.Find(triageProjectId)?.Stage == ProjectStage.Lead;

    private string? StagedTenderEnquiryProblem =>
        stagedCreate is { Kind: StagedRecordKind.TenderEnquiry } sc
            ? sc.TenderEnquiry.Problem(TriageProjectIsLead)
            : null;

    private string? StagedCalendarEventProblem =>
        stagedCreate is { Kind: StagedRecordKind.CalendarEvent } stagedEvent
            ? stagedEvent.CalendarEvent.Problem
            : null;

    private string? StagedBuildingControlInspectionProblem =>
        stagedCreate is { Kind: StagedRecordKind.BuildingControlInspection } stagedInspection
            ? stagedInspection.BuildingControlInspection.Problem
            : null;

    private string? TodoProjectNote =>
        string.IsNullOrWhiteSpace(triageProjectId)
            ? "No project set on the email — these will be company-wide items. Set the Project in the triage bar above to put them on a project's To-do tab."
            : $"Items land on the To-do tab of {ProjectLabelFor(triageProjectId)} — the email's project, set in the triage bar above.";

    // The assignee picker's option pool: the ROLES a to-do can be assigned to
    // (TodoRoles.AssignableAsTodoAssignee, served by ListTodoAssignableRoles) and, under each
    // role, the directory holders it can be pinned to (ListTodoAssignablePeople) — fetched once
    // when the page loads, shaped by TodoAssigneePicker.BuildOptions and shared by every to-do
    // draft row. Assignment is picker-only.
    private IReadOnlyList<SearchSelect.Option> todoAssigneeOptions = Array.Empty<SearchSelect.Option>();
    private IReadOnlyList<TodoAssignablePerson> assignablePeople = Array.Empty<TodoAssignablePerson>();

    // "Forward to QS" (2026-08-22): everyone in the staff directory with the QS role.
    private IReadOnlyList<TodoAssignablePerson> QsRecipients =>
        assignablePeople.Where(person => person.Role == Role.QuantitySurveyor).ToList();

    // The drafts exactly as they will be posted. Built in one place so the count promised on the
    // summary and the batch the apply actually sends can never disagree.
    private List<TodoItemDraft> CurrentTodoDrafts() => createTodoRows
        .Where(row => !string.IsNullOrWhiteSpace(row.Title))
        .Select(row => new TodoItemDraft(
            row.Title.Trim(),
            NullIfBlank(row.Notes),
            ParseTodoAssignees(row.Assignees),
            ParseDate(row.Due)))
        .ToList();

    private async Task LoadTodoAssignableRolesAsync()
    {
        // A failed load leaves the picker with no options rather than blocking triage — to-dos can
        // still be created, they just go in unassigned.
        try
        {
            var rolesTask = Todos.ListAssignableRolesAsync();
            var peopleTask = Todos.ListAssignablePeopleAsync();
            assignablePeople = await peopleTask;
            todoAssigneeOptions = TodoAssigneePicker.BuildOptions(await rolesTask, assignablePeople);
            await StageFilter.EnsureLoadedAsync();
            StageFilter.OnChange += StageFilterChanged;
        }
        catch { }
    }

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
        sessionReady = true;
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

    private void ToggleFilterMenu() => filterOpen = !filterOpen;

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

    private string FilterButtonLabel => selectedTags.Count == 0 ? "All tags" : $"{selectedTags.Count} selected";

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

    private string PathwayFilterChipClass(string? bucket)
    {
        var baseClass = "rounded-full border px-3 py-1 text-xs font-medium transition";
        return pathwayBucketFilter == bucket
            ? $"{baseClass} border-accent bg-surface-raised text-content"
            : $"{baseClass} border-line text-content-muted hover:text-content hover:border-line-strong";
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

    // ---- Parked triage (2026-08-10): navigating away no longer costs drafted work. ----
    // Whatever was staged on the open email — the written reply, the project choice, the picked
    // tags, the lined-up actions and to-dos — is parked under that email's id whenever the
    // selection moves off it, and put back exactly as it was the next time the email is opened.
    // Parking is in-memory only: a page refresh still starts clean ("Save reply as draft" is the
    // deliberate keep), and Apply consumes the work instead of parking it. An armed discard is
    // deliberately NOT parked — a destructive step should never come back pre-armed.
    private readonly Dictionary<string, ParkedTriage> parkedTriageByEmailId = new();

    // Everything drafted against one email, held while the triager reads elsewhere.
    private sealed class ParkedTriage
    {
        public TriagePathway? Pathway { get; init; }
        public string ReplyBody { get; init; } = "";
        public string ReplyToField { get; init; } = "";
        public string ReplyCcField { get; init; } = "";
        public string ReplyBccField { get; init; } = "";
        public string ReplySubject { get; init; } = "";
        public bool ReplyShowBcc { get; init; }
        public bool ReplyOpen { get; init; }
        public bool ReplyIsForward { get; init; }
        public bool ReplyEnvelopePrefilled { get; init; }
        public IReadOnlyList<ComposeDraftAttachment> ReplyAttachments { get; init; } = Array.Empty<ComposeDraftAttachment>();
        public string ProjectId { get; init; } = "";
        public bool ProjectAutoMatched { get; init; }
        public RecordType LinkRecordType { get; init; }
        public IReadOnlyList<LinkableRecord> PickedRecords { get; init; } = Array.Empty<LinkableRecord>();
        public IReadOnlyList<StagedSystemAction> SystemActions { get; init; } = Array.Empty<StagedSystemAction>();
        public StagedRecordCreate? Create { get; init; }
        // Records already raised from this email (Create now / the apply's create) — real
        // server-side facts, so the chips must come back with the email they belong to.
        public IReadOnlyList<CreatedNowRecord> CreatedRecords { get; init; } = Array.Empty<CreatedNowRecord>();
        public List<TodoDraftRow> TodoRows { get; init; } = new();
        // Nullable like the live fields: a parked email keeps its answered-or-blank state, so a
        // deliberate No survives a selection change exactly like a Yes — and an unanswered pair
        // comes back still demanding an answer.
        public bool? RelevantEventStaged { get; init; }
        public bool? TriageEntireThread { get; init; }
        public bool? UseThreadTags { get; init; }
        // Attachment ids ticked "Send to document triage" — drafted against ONE email's
        // attachments, so they must travel with that email like every other draft (the same
        // rule that kept the old save-to-drawings form from leaking under the next email).
        public IReadOnlyList<string> DocControlIds { get; init; } = Array.Empty<string>();
    }

    // Anything the triager has actually set is worth keeping; an untouched email parks nothing.
    private bool HasTriageWorthParking =>
        replyOpen
        || HtmlHasContent(replyBody)
        || replyAttachments.Count > 0
        || pickedRecords.Count > 0
        || stagedSystemActions.Count > 0
        || stagedCreate is not null
        || createdNowRecords.Count > 0
        || createTodoRows.Any(row => !string.IsNullOrWhiteSpace(row.Title) || !string.IsNullOrWhiteSpace(row.Notes))
        // An ANSWERED Yes/No is a decision the triager made — a No as much as a Yes — so either
        // pair being non-null is worth keeping across a selection change.
        || relevantEventStaged is not null
        || triageEntireThread is not null
        || useThreadTags is not null
        || stagedDocControlIds.Count > 0
        // The project counts only when the triager chose it themselves. An auto-matched project
        // (TryPrefillProjectFromEmailAsync's guess from the email text) is the page's doing, not
        // theirs — parking it made untouched emails show a "✎ draft" badge after a mere click-through.
        // Reopening the email re-runs the same auto-match anyway, so nothing is lost by not parking it.
        || (!projectAutoMatched && !string.IsNullOrWhiteSpace(triageProjectId));

    private void ParkSelectedTriage()
    {
        if (selected is null) return;
        if (!HasTriageWorthParking) return;
        parkedTriageByEmailId[selected.Id] = new ParkedTriage
        {
            Pathway = pathway,
            ReplyBody = replyBody,
            ReplyToField = replyToField,
            ReplyCcField = replyCcField,
            ReplyBccField = replyBccField,
            ReplySubject = replySubject,
            ReplyShowBcc = replyShowBcc,
            ReplyOpen = replyOpen,
            ReplyIsForward = replyIsForward,
            ReplyEnvelopePrefilled = replyEnvelopePrefilled,
            ReplyAttachments = replyAttachments,
            ProjectId = triageProjectId,
            ProjectAutoMatched = projectAutoMatched,
            LinkRecordType = linkRecordType,
            PickedRecords = pickedRecords.ToList(),
            SystemActions = stagedSystemActions.ToList(),
            Create = stagedCreate,
            CreatedRecords = createdNowRecords.ToList(),
            TodoRows = createTodoRows,
            RelevantEventStaged = relevantEventStaged,
            TriageEntireThread = triageEntireThread,
            UseThreadTags = useThreadTags,
            DocControlIds = stagedDocControlIds.ToList()
        };
    }

    // Put a parked email's work back exactly as it was left. Returns whether anything came back,
    // so Select knows to refetch the link-record pool the restored picks came from.
    private bool RestoreParkedTriage(string emailId)
    {
        if (!parkedTriageByEmailId.Remove(emailId, out var parked)) return false;
        pathway = parked.Pathway;
        replyBody = parked.ReplyBody;
        replyToField = parked.ReplyToField;
        replyCcField = parked.ReplyCcField;
        replyBccField = parked.ReplyBccField;
        replySubject = parked.ReplySubject;
        replyShowBcc = parked.ReplyShowBcc;
        replyOpen = parked.ReplyOpen;
        replyIsForward = parked.ReplyIsForward;
        replyEnvelopePrefilled = parked.ReplyEnvelopePrefilled;
        replyAttachments = parked.ReplyAttachments;
        triageProjectId = parked.ProjectId;
        projectAutoMatched = parked.ProjectAutoMatched;
        linkRecordType = parked.LinkRecordType;
        pickedRecords.AddRange(parked.PickedRecords);
        stagedSystemActions.AddRange(parked.SystemActions);
        stagedCreate = parked.Create;
        createdNowRecords.AddRange(parked.CreatedRecords);
        createTodoRows = parked.TodoRows;
        relevantEventStaged = parked.RelevantEventStaged;
        triageEntireThread = parked.TriageEntireThread;
        useThreadTags = parked.UseThreadTags;
        stagedDocControlIds.AddRange(parked.DocControlIds);
        return true;
    }

    // LoadLinkRecordsAsync clears the picks because a new pool normally invalidates them —
    // restored picks are the exception, so they are carried over the reload by hand.
    private async Task ReloadLinkRecordsKeepingPicksAsync()
    {
        var restoredPicks = pickedRecords.ToList();
        await LoadLinkRecordsAsync();
        pickedRecords.AddRange(restoredPicks);
    }

    // Point the detail pane at an email: park whatever was drafted on the previous one, set the
    // selection and reset the forms — then, if this email itself was parked earlier, put its
    // work back. Loading the body and thread is the caller's job — Select does both.
    private bool ApplySelection(MailboxMessage item)
    {
        ParkSelectedTriage();
        selected = item;
        // Pathway-first: pre-select the thread's own pathway when it already carries one (rendered as
        // a fixed badge); otherwise the triager chooses before any action beyond Discard is offered.
        pathway = PathwayFromBucket(item.Bucket);
        actionError = null;
        ResetLinkState();
        composeOutcome = null;
        linkNote = null;
        poEmailNote = null;
        // The Outbox's own state deliberately survives here: queuedReplies and the open composer
        // anchor are workspace-level, not per-selection staging. Only the outcome banner clears.
        outboxNote = null;
        replyBody = "";
        replyToField = replyCcField = replyBccField = replySubject = "";
        replyShowBcc = false;
        replyOpen = false;
        replyIsForward = false;
        replyEnvelopePrefilled = false;
        replyAttachments = Array.Empty<ComposeDraftAttachment>();
        triageProjectId = "";
        projectAutoMatched = false;
        createTodoRows = new List<TodoDraftRow> { new() };
        stagedSystemActions.Clear();
        discardArmed = false;
        stagedCreate = null;
        createdNowRecords.Clear();
        relevantEventStaged = null;
        triageEntireThread = null;
        useThreadTags = null;
        // The document-triage ticks are drafted against ONE email's attachments — leaving them
        // across a selection change would send another email's attachment ids against this
        // message. Parked above, reset here, restored below like every other per-email draft.
        stagedDocControlIds.Clear();
        return RestoreParkedTriage(item.Id);
    }

    // Open the clicked email exactly as clicked — the selection stays on it; the thread panel
    // below still shows any newer replies for context. Actions apply to just this email unless
    // a thread-wide Yes on the "Entire thread" decision opts the apply into the whole conversation.
    private async Task Select(MailboxMessage item)
    {
        var restoredParkedWork = ApplySelection(item);
        // The email reads in the window OPPOSITE the list, side by side — desktop's version of
        // the old list/detail split, but with both halves loadable anywhere.
        workspace.ShowOpposite(PanelKind.Email, PanelKind.Inbox);
        // Body and thread are independent live reads — fetch them side by side.
        await Task.WhenAll(LoadDetailAsync(item), LoadThreadAsync(item));
        // A restored draft brings its record picks back, so the pool they came from is refetched.
        if (restoredParkedWork && !string.IsNullOrWhiteSpace(triageProjectId))
            await ReloadLinkRecordsKeepingPicksAsync();
        // Both reads have landed, so the whole chain is available to search for a project name
        // to pre-fill the pickers with.
        await TryPrefillProjectFromEmailAsync();
    }

    // Fetch the full body + attachment names on demand when an email is opened. Cancels any in-flight
    // fetch so rapid clicking can't race a stale result onto the newly selected email.
    private async Task LoadDetailAsync(MailboxMessage item)
    {
        detailCts?.Cancel();
        var cts = new CancellationTokenSource();
        detailCts = cts;

        detail = null;
        detailLoading = true;
        try
        {
            var loaded = await Intake.GetMessageDetailAsync(item.Id, item.InternetMessageId, cts.Token);
            if (!cts.IsCancellationRequested && selected?.Id == item.Id)
            {
                detail = loaded;
                PrefillReplyEnvelope(item, loaded);
                ReflectLiveTags(loaded);
            }
        }
        catch (OperationCanceledException) { /* superseded by a newer selection */ }
        catch { /* leave detail null so the view falls back to the preview */ }
        finally
        {
            if (selected?.Id == item.Id)
                detailLoading = false;
        }
    }

    // The open email's tags as the mailbox holds them NOW, carried on the detail read, replace the
    // copy the list page gave us: a row goes stale the moment something tags the email while it
    // stays open — System Actions' Create now raising a record from it (2026-08-25: the defect was
    // raised and the email tagged, but the Control Centre kept showing it untagged). The selected
    // record and its list row are both swapped, so the queue row grows its chips and the pane's
    // pathway follows the thread's real filing. A detail that couldn't read the tags changes nothing.
    private void ReflectLiveTags(MailboxMessageDetail loaded)
    {
        if (loaded.Categories is null || selected is null || selected.Id != loaded.MessageId) return;
        var hasSameTags = selected.Categories.SequenceEqual(loaded.Categories)
            && string.Equals(selected.Bucket, loaded.Bucket, StringComparison.OrdinalIgnoreCase);
        if (hasSameTags) return;

        var refreshed = selected with { Categories = loaded.Categories, Bucket = loaded.Bucket };
        selected = refreshed;
        if (refreshed.Bucket is not null) pathway = PathwayFromBucket(refreshed.Bucket);
        items = ReplaceRow(items, refreshed);
        taggedItems = ReplaceRow(taggedItems, refreshed);
        discardedItems = ReplaceRow(discardedItems, refreshed);
        thread = ReplaceRow(thread, refreshed);
    }

    private static IReadOnlyList<MailboxMessage> ReplaceRow(IReadOnlyList<MailboxMessage> rows, MailboxMessage refreshed) =>
        rows.Any(row => row.Id == refreshed.Id)
            ? rows.Select(row => row.Id == refreshed.Id ? refreshed : row).ToList()
            : rows;

    // Re-read the open email's tags after an act that tagged it server-side while it stays selected
    // (Create now). Only the tags are refreshed — the body, thread and every draft on the email are
    // untouched, so nothing flickers and nothing staged is lost. Best-effort: a failed read leaves
    // the row as it was, and Apply's queue reload reconciles it anyway.
    private async Task RefreshSelectedTagsAsync(MailboxMessage anchor)
    {
        try
        {
            var loaded = await Intake.GetMessageDetailAsync(anchor.Id, anchor.InternetMessageId);
            ReflectLiveTags(loaded);
        }
        catch { /* the row stays as it was; the next queue reload shows the truth */ }
    }

    // Fetch the selected email's whole conversation for the thread panel. Same cancellation shape as
    // LoadDetailAsync: rapid clicking can't race a stale thread onto the newly selected email. The
    // list is cleared up front so a previous selection's thread never flashes against this one.
    private async Task LoadThreadAsync(MailboxMessage item)
    {
        threadCts?.Cancel();
        var cts = new CancellationTokenSource();
        threadCts = cts;

        thread = Array.Empty<MailboxMessage>();
        threadMatchedBySubject = false;
        threadError = null;
        if (string.IsNullOrEmpty(item.ConversationId)) { threadLoading = false; return; }

        threadLoading = true;
        try
        {
            var page = await Intake.ListConversationLiveAsync(item.ConversationId, item.Subject, cts.Token);
            if (cts.IsCancellationRequested || selected?.Id != item.Id)
                return;
            thread = page.Items;
            threadMatchedBySubject = page.MatchedBySubject;
        }
        catch (OperationCanceledException) { /* superseded by a newer selection */ }
        catch
        {
            if (selected?.Id == item.Id)
                threadError = "Couldn't read this email's thread — the conversation may still have replies.";
        }
        finally
        {
            if (selected?.Id == item.Id)
                threadLoading = false;
        }
    }

    // ---- Project auto-match ----
    // A simple lower-case search of the email chain for a project's name: when exactly one live
    // project's name appears verbatim (case-insensitive) in the selected email's subject, body or
    // thread, the project pickers are pre-filled with it. The triager still sees — and can change —
    // the choice; an ambiguous chain (two project names in one thread) pre-fills nothing, and a
    // choice already made is never overridden.
    private async Task TryPrefillProjectFromEmailAsync()
    {
        if (view != QueueView.Active || selected is null) return;
        if (!string.IsNullOrWhiteSpace(triageProjectId)) return;

        var haystack = BuildEmailSearchText();
        if (haystack.Length == 0) return;

        // Live projects only (the pickers hide completed ones by default), and names under four
        // characters are skipped — too short to be an honest match rather than a coincidence.
        var matches = AllProjects
            .Where(project => project.Stage != ProjectStage.Completed)
            .Where(project => project.Name.Trim() is { Length: >= 4 } name
                && haystack.Contains(name.ToLowerInvariant(), StringComparison.Ordinal))
            .ToList();
        if (matches.Count != 1) return;

        triageProjectId = matches[0].ProjectId;
        projectAutoMatched = true;
        // The link panel shows records for its chosen project, so the pre-fill loads them too —
        // otherwise it would claim "no records on this project yet" without having looked.
        await LoadLinkRecordsAsync();
    }

    // Everything searchable about the selected email's chain, joined and lower-cased once: subject,
    // preview, the fetched body (tags stripped when HTML) and every thread member's subject/preview.
    private string BuildEmailSearchText()
    {
        var parts = new List<string?> { selected?.Subject, selected?.BodyPreview };
        if (detail is not null)
            parts.Add(detail.BodyIsHtml ? StripHtml(detail.BodyHtml) : detail.BodyHtml);
        foreach (var member in thread)
        {
            parts.Add(member.Subject);
            parts.Add(member.BodyPreview);
        }
        return string.Join("\n", parts.Where(part => !string.IsNullOrWhiteSpace(part))).ToLowerInvariant();
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html ?? "", "<[^>]*>", " ");

    private static string ThreadRowClass(bool current)
    {
        var baseClass = "w-full text-left rounded-lg border px-3 py-2 transition";
        return current
            ? $"{baseClass} border-accent bg-surface"
            : $"{baseClass} border-line hover:border-line-strong hover:bg-surface";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:0.#} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes} B";
    }

    // Choose the pathway for a not-yet-filed thread (staging from a pathway pane). Staged picks
    // deliberately SURVIVE the switch — the modal shows its running total across every tab, and a
    // genuine cross-pathway combination simply files the thread under both at apply (the confirm
    // was retired 2026-08-28). A thread that already carries a pathway ignores this — its routing
    // was decided at first filing.
    private void SetPathway(TriagePathway next)
    {
        if (FixedPathway is not null || pathway == next) return;
        pathway = next;
        actionError = null;
    }

    private async Task OnLinkRecordTypeChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<RecordType>(e.Value?.ToString(), out var type)) linkRecordType = type;
        linkRecordId = "";
        // pickedRecords deliberately survive a type switch — that's how one email links to
        // records of several types in one apply.
        await LoadLinkRecordsAsync();
    }

    // One landing for the email's project decision: the triage bar's ProjectSelect hands the id
    // straight in; the Tagged view's plain <select> still speaks ChangeEventArgs and forwards.
    private async Task OnTriageProjectPicked(string projectId)
    {
        triageProjectId = projectId;
        projectAutoMatched = false; // an explicit choice replaces the guess
        linkRecordId = "";
        pickedRecords.Clear();
        await LoadLinkRecordsAsync();
    }

    private async Task OnTriageProjectChanged(ChangeEventArgs e) =>
        await OnTriageProjectPicked(e.Value?.ToString() ?? "");

    // Load the chosen project's records of the chosen type for the picker. Record-agnostic: the same
    // call backs both the Link panel and the Tagged tab's "link to another record" control.
    private async Task LoadLinkRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(triageProjectId))
        {
            linkRecords = Array.Empty<LinkableRecord>();
            return;
        }
        try
        {
            linkRecordsLoading = true;
            linkRecordId = "";
            pickedRecords.Clear();
            StateHasChanged(); // show the loading state while the fetch is in flight
            linkRecords = await Intake.ListLinkableRecordsAsync(triageProjectId, linkRecordType);
        }
        catch
        {
            linkRecords = Array.Empty<LinkableRecord>();
        }
        finally
        {
            linkRecordsLoading = false;
        }
    }

    // The first record type the current context offers (the chosen pathway on the queue, the thread's
    // bucket on the Tagged tab) — what the link picker resets to, so a type from another pathway can
    // never survive a pathway or selection change.
    private RecordType DefaultLinkRecordType => view == QueueView.Tagged
        ? TaggedLinkTypeOptions[0]
        : (QueueLinkTypeOptions.Count > 0 ? QueueLinkTypeOptions[0] : RecordType.Request);

    // Clear the link picker back to the current context's defaults after a selection or pathway
    // changes, or a link action completes.
    // Clears the record picks and pool — NOT the project: the project is the email's own global
    // step and survives pathway/action switches.
    private void ResetLinkState()
    {
        linkRecordType = DefaultLinkRecordType;
        linkRecordId = "";
        pickedRecords.Clear();
        linkRecords = Array.Empty<LinkableRecord>();
    }

    // The pathway label sent with a link command. Only pathway-neutral COST-CENTRE links carry one —
    // the record type implies the pathway everywhere else, and a Todo link must stay neutral (sending
    // a pathway with it would file the thread, which a to-do never does). On the queue it is the
    // triager's selection; on the Tagged tab it is the thread's own side. Internal never applies —
    // cost-centre mail is valuation-side (Client) or subcontract-side (Subcontractor) only.
    private string? CostCentrePathwayFor(LinkableRecord record)
    {
        if (record.Type != RecordType.CostCentre) return null;
        var side = view == QueueView.Tagged ? FixedPathway : pathway;
        return side is TriagePathway.Client or TriagePathway.Subcontractor ? PathwayLabel(side.Value) : null;
    }

    // "Reply in thread": the reply written here is staged as an Outlook draft on the email
    // (projects mailbox, thread quoted behind it) AND becomes the description of a General request
    // created from it in the background — one write-up answers the email and papers the request, so
    // the email is triaged by the act of replying. The outcome (request + draft weblink) is kept
    // for the success banner; the pre-filled draft is reviewed and sent from Outlook itself.
    // ---- Reply compose (send for real — decision 2026-08-04) ----

    // Reply-all prefill, computed once per selection from the opened email's envelope: the sender
    // (or their Reply-To) goes in To; the original To + Cc — minus whoever is now in To — go in Cc.
    // The projects mailbox itself is filtered out — Cc'ing it would deliver a copy back to the
    // Inbox and land it in the triage queue (decision 2026-08-07: no auto-Cc anywhere).
    private void PrefillReplyEnvelope(MailboxMessage item, MailboxMessageDetail loaded)
    {
        if (replyEnvelopePrefilled) return;
        // A forward's envelope is deliberately blank (FW subject already set) — the late-landing
        // detail must not overwrite it with the reply-all prefill.
        if (replyIsForward) return;

        var toAddress = loaded.ReplyTo ?? loaded.FromEmail ?? item.FromEmail;
        replyToField = toAddress ?? "";

        var ccAddresses = (loaded.To ?? Array.Empty<string>())
            .Concat(loaded.Cc ?? Array.Empty<string>())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Where(a => !a.Equals(toAddress, StringComparison.OrdinalIgnoreCase))
            // Strip the projects mailbox from the prefill: replying with it on Cc would deliver
            // the sent email back into the Inbox, where it lands in the triage queue again.
            .Where(a => loaded.MailboxAddress is null || !a.Equals(loaded.MailboxAddress, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        replyCcField = string.Join("; ", ccAddresses);

        var subject = loaded.Subject ?? item.Subject;
        replySubject = string.IsNullOrWhiteSpace(subject) ? "RE: (no subject)"
            : subject.TrimStart().StartsWith("RE:", StringComparison.OrdinalIgnoreCase) ? subject.Trim()
            : $"RE: {subject.Trim()}";

        replyEnvelopePrefilled = true;
    }

    private bool ReplyIsSendable =>
        ParseRecipients(replyToField).Count > 0
        && !string.IsNullOrWhiteSpace(replySubject)
        && HtmlHasContent(replyBody);

    // True while a reply is drafted but unsent — the filing panes' own buttons stand down so a
    // written reply can't be silently left behind by a tag-only action; Send applies both.
    private bool ReplyDraftPending => replyOpen && HtmlHasContent(replyBody);

    // The email's global project, giving the attachment picker its context.
    private string ComposeContextProjectId => triageProjectId;

    // What Send will do besides sending, phrased for the note above the button. Null = nothing.
    // Everything the action bar will do, phrased as one sentence ("send your reply, raise 2
    // to-dos and link this email to the selected record"). Null = nothing pending, button disabled.
    private string? PendingSummary
    {
        get
        {
            var parts = new List<string>();
            if (ReplyDraftPending)
                parts.Add(replyIsForward ? "send your forward" : "send your reply");
            if (queuedReplies.Count > 0)
                parts.Add(queuedReplies.Count == 1
                    ? $"send the lined-up {(queuedReplies[0].IsForward ? "forward" : "reply")} to {queuedReplies[0].AnchorFrom}"
                    : $"send {queuedReplies.Count} lined-up emails");
            var todoCount = CurrentTodoDrafts().Count;
            if (todoCount > 0)
                parts.Add(todoCount == 1 ? "raise the to-do" : $"raise {todoCount} to-dos");
            if (pickedRecords.Count > 0)
                parts.Add(pickedRecords.Count == 1
                    ? $"link this email to {pickedRecords[0].Reference}"
                    : $"link this email to {string.Join(", ", pickedRecords.Take(3).Select(r => r.Reference))}{(pickedRecords.Count > 3 ? $" +{pickedRecords.Count - 3} more" : "")}");
            if (useThreadTags == true && SelectedThreadTags is { Count: > 0 } inheritStems)
                parts.Add(inheritStems.Count == 1
                    ? $"file it under the thread's existing tag ({TagLabel(inheritStems[0])})"
                    : $"file it under the thread's existing tags ({string.Join(", ", inheritStems.Take(3).Select(TagLabel))}{(inheritStems.Count > 3 ? $" +{inheritStems.Count - 3} more" : "")})");
            if (stagedDocControlIds.Count > 0)
                parts.Add(stagedDocControlIds.Count == 1
                    ? "send 1 attachment to Document Triage"
                    : $"send {stagedDocControlIds.Count} attachments to Document Triage");
            if (StagedCreateReady && (!string.IsNullOrWhiteSpace(triageProjectId) || StagedCreatesOwnProject))
                parts.Add(stagedCreate!.Kind switch
                {
                    StagedRecordKind.BidPackage => "create the bid package from this email",
                    StagedRecordKind.TenderEnquiry => StagedCreatesOwnProject
                        ? "create a Lead project and log the tender enquiry from this email"
                        : "log the tender enquiry from this email",
                    StagedRecordKind.WorkOrder => StagedWorkOrderSummary(stagedCreate),
                    StagedRecordKind.Defect => "raise the defect from this email",
                    StagedRecordKind.Inventory => "add the inventory item from this email",
                    StagedRecordKind.CalendarEvent => "raise the calendar event from this email",
                    StagedRecordKind.BuildingControlInspection => "raise the building control inspection from this email",
                    _ => stagedCreate.RequestKind == RequestType.Rfi
                        ? "raise the RFI from this email"
                        : "create the request from this email"
                });
            if (stagedSystemActions.Count == 1)
                parts.Add($"run 1 system action ({stagedSystemActions[0].Summary})");
            else if (stagedSystemActions.Count > 1)
                parts.Add($"run {stagedSystemActions.Count} system actions");
            if (relevantEventStaged == true)
                parts.Add("tag it a Relevant Event for the Programme");
            if (discardArmed)
                parts.Add("discard this email and its thread");
            // Create now already raised the record and tagged the email — with nothing else
            // staged, the apply's one remaining job is clearing the dealt-with email from the
            // queue. Without this clause Apply sat disabled after a create-now-only triage
            // ("it expects a staged tag") and the email was stuck open (reported 2026-08-28).
            if (parts.Count == 0 && selected is not null && createdNowRecords.Count > 0)
                return createdNowRecords.Count == 1
                    ? $"clear this email from the queue — {createdNowRecords[0].Reference} is already raised and the email tagged to it"
                    : $"clear this email from the queue — {string.Join(", ", createdNowRecords.Select(r => r.Reference))} are already raised and the email tagged to them";
            if (parts.Count == 0) return null;
            var summary = parts.Count == 1 ? parts[0] : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];
            // A thread-wide Yes changes what an apply MEANS, so the sentence says so — but only when
            // something staged actually spreads (a bare discard is thread-wide regardless).
            if (triageEntireThread == true && !(discardArmed && parts.Count == 1))
                summary += " — covering every email currently in this thread";
            // Lined-up replies inherit the triage's record picks: the sentence says so, because
            // tagging OTHER emails is the one effect a reader wouldn't otherwise expect.
            if (queuedReplies.Count > 0 && pickedRecords.Count > 0)
                summary += $" (the lined-up {(queuedReplies.Count == 1 ? "email's anchor is" : "emails' anchors are")} tagged to the picked records too)";
            return summary;
        }
    }

    // The staged work order phrased for the apply note, counting the record-keeping attachments
    // (ticked email files + picked uploads) it will keep on the new order.
    private static string StagedWorkOrderSummary(StagedRecordCreate staged)
    {
        var label = staged.SaveAsDraft
            ? "raise the draft work order from this email"
            : "raise the work order from this email and email the purchase order to the subcontractor";
        var attachmentCount = staged.EmailAttachmentIds.Count + staged.UploadFiles.Count;
        return attachmentCount == 0
            ? label
            : $"{label} (keeping {attachmentCount} attachment{(attachmentCount == 1 ? "" : "s")} on the order — not emailed)";
    }

    // Done on a pathway pane: confirm the picks and land that window back on the open
    // email — the same place every time. The plain pane-history fallback ("whatever this window
    // showed before") read as a bug in practice: with System Actions earlier in the history,
    // Done appeared to open the RFI form out of nowhere (reported 2026-08-20). Close() first so
    // SystemTags leaves the history entirely — closing the email later must not resurface a
    // confirmed tags window and silently re-block Apply. When the email is already on show in
    // the other window, the plain close is enough — no point opening a mirror copy over here.
    private void ClosePathwayPane(PanelKind pane)
    {
        var side = workspace.SideShowing(pane);
        workspace.Close(pane);
        if (side is not { } paneSide || selected is null) return;
        // On mobile only the left pane is on screen, so the right pane "showing" the email
        // doesn't count as the email being visible — bring it to the one real window.
        var emailVisible = workspace.IsDesktop && workspace.SideShowing(PanelKind.Email) is not null;
        if (!emailVisible) workspace.Show(PanelKind.Email, paneSide);
    }

    // An action just closed the open email (applied, discarded, restored, re-tagged), so the
    // email window and its reading copy have nothing left to show. Bring the queue list back on
    // show wherever the panes were left — without this, an apply run while the mirror covered
    // the inbox landed on two empty windows with the list nowhere in sight (reported
    // 2026-08-28: "loaded without the mailbox selected"). The mirror closes outright (a reading
    // copy of nothing has no reason to wait in the history); the inbox then either resurfaces
    // from that pane's own history or is shown on the left, its home side.
    private void ReturnWorkspaceToQueue()
    {
        workspace.Close(PanelKind.EmailMirror);
        if (workspace.SideShowing(PanelKind.Inbox) is null)
            workspace.Show(PanelKind.Inbox, PanelSide.Left);
    }

    // NOTE (2026-08-27): the old "Apply stands down while a tags window is open" rule is GONE.
    // It fit the one modal System Tags pane; with four standing pathway panes (which also host
    // browsable registers) it left Apply disabled almost permanently, with the reason buried in
    // a tooltip — Nigel filled everything in, pressed Done everywhere he could see, and still
    // couldn't apply. Picks and ticks stage LIVE into the page's one list, and every staged
    // record form is readiness-checked by DoApplyAll itself, so an open pane holds nothing back.

    // True while either of the bar's Yes/No pairs is still blank for the open email. Apply (and
    // save-as-drafts) stand down until both are answered — the pairs deliberately start with
    // NEITHER side picked, so tagging the programme and sweeping the thread are always decisions
    // someone actually made, never a default that slipped through.
    private bool TriageDecisionsMissing =>
        selected is not null && MissingDecisionNames().Count > 0;

    // The blank pairs still awaiting an answer, by their on-screen names — one list feeds both
    // the amber hint next to Apply and the belt-and-braces error inside DoApplyAll, so the two
    // can never drift. "Use existing tags" counts only while its row is on show (the thread
    // actually carries tags to inherit).
    private List<string> MissingDecisionNames()
    {
        var missing = new List<string>();
        if (relevantEventStaged is null) missing.Add("Relevant Event for Programme");
        if (triageEntireThread is null) missing.Add("Entire thread");
        if (SelectedThreadTags.Count > 0 && useThreadTags is null) missing.Add("Use existing tags");
        return missing;
    }

    private static string AndJoin(IReadOnlyList<string> parts) =>
        parts.Count <= 1
            ? parts.FirstOrDefault() ?? ""
            : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];

    // The record tags the open email's thread already carries — the queue row's outline "Thread:"
    // chips, populated only on queue listings (a new reply to an already-linked thread). Empty
    // everywhere else, so the "Use existing tags" row and its gate simply don't exist there.
    private IReadOnlyList<string> SelectedThreadTags =>
        selected?.ThreadTags is { Count: > 0 } tags ? tags : Array.Empty<string>();

    // True while attachments are ticked for Document Triage but the email has no project.
    // The project is REQUIRED for a Document Triage send (decision 2026-08-28): a file landing
    // in the queue with no project is as good as discarded, and the triage bar — where the
    // email says which job it is — is the cheapest place to set it. Same standing-hint
    // treatment as the Yes/No pairs (2026-08-27: the disable reason stands next to the button).
    private bool DocTriageProjectMissing =>
        selected is not null && stagedDocControlIds.Count > 0 && string.IsNullOrWhiteSpace(triageProjectId);

    private const string DocTriageProjectMissingHint =
        "Set the Project first — attachments can't go to Document Triage without one";

    private string DecisionsMissingHint =>
        $"Answer {AndJoin(MissingDecisionNames())} — Yes or No — first";

    // The bar's Yes/No pair: two joined pill halves, neither lit until the triager picks a side
    // (null = blank). Picked reads like a picked record row (accent border on raised surface);
    // the unpicked side stays muted. Clicking the picked side again is a no-op, not a clear —
    // the whole point is that "no answer" isn't a state anyone can put back.
    private static string YesNoClass(bool? decided, bool answer, bool first) =>
        "px-2.5 py-1 text-xs border transition "
        + (first ? "rounded-l-lg" : "rounded-r-lg -ml-px")
        + (decided == answer
            ? " relative border-accent bg-surface-raised text-content font-medium"
            : " border-line text-content-subtle hover:text-content hover:border-line-strong");

    private string ApplyButtonLabel
    {
        get
        {
            var filing = CurrentTodoDrafts().Count > 0
                || pickedRecords.Count > 0
                || (useThreadTags == true && SelectedThreadTags.Count > 0)
                || relevantEventStaged == true
                || stagedSystemActions.Count > 0
                || stagedDocControlIds.Count > 0
                || (StagedCreateReady && (!string.IsNullOrWhiteSpace(triageProjectId) || StagedCreatesOwnProject));
            var sendCount = (ReplyDraftPending ? 1 : 0) + queuedReplies.Count;
            if (sendCount > 0)
            {
                var send = sendCount == 1 ? "Send reply" : $"Send {sendCount} replies";
                return filing ? $"{send} & file" : send;
            }
            return discardArmed && !filing ? "Discard email" : "Apply";
        }
    }

    // Open the composer under the open email as a reply or a forward. The two kinds prime the
    // envelope differently — reply-all prefill vs a blank envelope with a "FW:" subject — so
    // switching kind re-primes it (the written body and any extra attachments survive; original
    // attachments picked for a reply are dropped on a switch to forward, because Graph carries
    // the originals on a forward draft automatically).
    private void OpenReplyComposer(bool forward)
    {
        replyOpen = true;
        if (replyIsForward == forward) return;
        replyIsForward = forward;
        replyShowBcc = false;
        if (forward)
        {
            replyToField = replyCcField = replyBccField = "";
            replySubject = MailCompose.ForwardSubjectFor(detail?.Subject ?? selected?.Subject);
            replyAttachments = replyAttachments
                .Where(a => a.Source != ComposeAttachmentSource.OriginalMessage)
                .ToList();
        }
        else
        {
            replyToField = replyCcField = replyBccField = "";
            replySubject = "";
            replyEnvelopePrefilled = false;
            if (selected is { } item && detail is { } loaded) PrefillReplyEnvelope(item, loaded);
        }
    }

    private void DiscardReplyDraft()
    {
        replyOpen = false;
        replyBody = "";
        replyAttachments = Array.Empty<ComposeDraftAttachment>();
        // A discarded forward hands the composer back in reply shape, reply-all re-prefilled, so
        // the next "↩ Reply" press starts from the normal envelope.
        if (replyIsForward)
        {
            replyIsForward = false;
            replyToField = replyCcField = replyBccField = "";
            replySubject = "";
            replyEnvelopePrefilled = false;
            if (selected is { } item && detail is { } loaded) PrefillReplyEnvelope(item, loaded);
        }
    }

    // The shared composer rules (MailCompose), aliased so every call site here reads the same as
    // it always did — the logic itself is defined once for all mail-writing surfaces.
    private static bool HtmlHasContent(string html) => MailCompose.HtmlHasContent(html);

    private void OnReplyAttachmentsChanged(IReadOnlyList<ComposeDraftAttachment> attachments) =>
        replyAttachments = attachments;

    private static IReadOnlyList<(string PartName, Microsoft.AspNetCore.Components.Forms.IBrowserFile File)> UploadPartsOf(
        IReadOnlyList<ComposeDraftAttachment> attachments) => MailCompose.UploadPartsOf(attachments);

    private static List<ComposeRecipient> ParseRecipients(string field) => MailCompose.ParseRecipients(field);

    // ONE Send: applies whatever filing is set up in the panes above (record links, a new
    // record, to-dos), then sends the reply. Filing and replying are two halves of dealing with
    // an email — deliberately combinable, never forced apart. Filing runs first (each command
    // verifies its tags before saving); the send comes last so a filing failure stops everything
    // with the email still queued, and a send failure leaves the thread filed with the reply
    // safe in Drafts (the outcome banner says exactly which).
    // THE action: applies everything the three sections have set up — the reply (section 1),
    // the to-do drafts (section 2) and the record filing (section 3) — in one click, in that
    // order. Filing runs first (every tag verified before anything saves); the send comes last so
    // a filing failure stops everything with the email still queued, and a send failure leaves the
    // thread filed with the reply safe in Drafts (the outcome banner says exactly which).
    private async Task DoApplyAll(bool saveAsDraftOnly)
    {
        if (busy) return;
        // Lined-up Outbox replies apply on their own — no selection needed (decision 2026-08-12).
        if (selected is null && queuedReplies.Count == 0) return;

        var anchorEmail = selected;
        var replying = ReplyDraftPending && anchorEmail is not null;
        var drafts = anchorEmail is null ? new List<TodoItemDraft>() : CurrentTodoDrafts();
        var picks = pickedRecords.ToList();
        var createReady = anchorEmail is not null && StagedCreateReady
            && (!string.IsNullOrWhiteSpace(triageProjectId) || StagedCreatesOwnProject);
        var relevantEvent = relevantEventStaged == true && anchorEmail is not null;
        var discarding = discardArmed && anchorEmail is not null;
        // "Use existing tags" answered Yes: the thread's tag stems, captured now so the apply
        // works from what the triager saw. Resolved to records inside the try — before anything
        // else lands — and linked exactly like picks.
        var inheritStems = useThreadTags == true && anchorEmail is not null
            ? SelectedThreadTags.ToList()
            : new List<string>();
        // One scope for the whole apply: a thread-wide Yes opts every staged action into the thread.
        var scope = triageEntireThread == true ? LinkThreadScope.EntireThread : LinkThreadScope.MessageOnly;
        // A create-now-only triage: the record is already raised and the email tagged to it
        // (Create now did both), so there is nothing left to RUN — but the apply still owns the
        // close-out below (queue reload, selection cleared). Letting it through is what
        // un-sticks the Apply button after Create now; every step in the body no-ops on its
        // own zero-count guard.
        var createdNowOnly = anchorEmail is not null && createdNowRecords.Count > 0;
        if (!replying && drafts.Count == 0 && picks.Count == 0 && !createReady && !relevantEvent && !discarding
            && stagedSystemActions.Count == 0 && queuedReplies.Count == 0 && stagedDocControlIds.Count == 0
            && inheritStems.Count == 0 && !createdNowOnly) return;

        // The bar's Yes/No pairs start blank on purpose — an apply with any unanswered is a
        // decision not yet made. Belt-and-braces behind the disabled button, so no other route
        // into the apply can land with a blank answer.
        if (anchorEmail is not null && MissingDecisionNames() is { Count: > 0 } missingDecisions)
        {
            actionError = $"Answer {AndJoin(missingDecisions)} — Yes or No — then Apply.";
            return;
        }

        // A half-built lined-up email is a decision not yet made — finish it or remove it, rather
        // than have Apply skip it (or the server reject it after the filing has already landed).
        if (queuedReplies.FirstOrDefault(lined => lined.Problem is not null) is { } notReady)
        {
            actionError = $"A lined-up {(notReady.IsForward ? "forward" : "reply")} ({notReady.AnchorSubject}) isn't ready — {notReady.Problem} Finish it in the Outbox, or remove it.";
            return;
        }

        // A reply and a discard contradict each other — an email worth answering isn't spam.
        // (Same rule for an unsent forward: send or discard the draft before binning the email.)
        if (discarding && replying)
        {
            actionError = $"Discard and a {(replyIsForward ? "forward" : "reply")} don't mix — send (or discard) the draft first.";
            return;
        }
        if (discarding && picks.Count > 0)
        {
            actionError = "Discard and record links don't mix — unpick the records first.";
            return;
        }
        if (discarding && inheritStems.Count > 0)
        {
            actionError = "Discard and the thread's existing tags don't mix — answer No to Use existing tags, or disarm the discard.";
            return;
        }
        if (discarding && relevantEvent)
        {
            actionError = "Discard and a Relevant Event tag don't mix — answer No to Relevant Event, or disarm the discard.";
            return;
        }
        // A Relevant Event answered Yes without a project is a decision not yet made — same rule as the
        // staged create: finish it or clear it, rather than have Apply quietly skip it.
        if (relevantEvent && string.IsNullOrWhiteSpace(triageProjectId))
        {
            actionError = "To tag a Relevant Event for the Programme, set the email's Project first — or answer No.";
            return;
        }

        // Attachments bound for Document Triage without a project are the same "decision not
        // yet made" (decision 2026-08-28): an unassigned file in the queue is as good as
        // discarded. Belt-and-braces behind the disabled button, like the Yes/No gate above.
        if (anchorEmail is not null && stagedDocControlIds.Count > 0 && string.IsNullOrWhiteSpace(triageProjectId))
        {
            actionError = "To send attachments to Document Triage, set the email's Project first — or untick them.";
            return;
        }

        // A staged new record without a project is a decision not yet made — finish it or clear
        // it, rather than have Apply quietly skip it.
        if (StagedCreateReady && !createReady)
        {
            actionError = "To create the record, set the email's Project first — or remove the staged record in the pathway pane's Actions.";
            return;
        }
        // A staged work order that isn't complete yet (no subcontractor, no priced line…) is the
        // same "decision not yet made": finish it or clear it, rather than let the server reject
        // a half-built order after the to-dos have already been raised.
        if (createReady && stagedCreate is { Kind: StagedRecordKind.WorkOrder } stagedOrder
            && stagedOrder.WorkOrderProblem is { } orderProblem)
        {
            actionError = $"The staged work order isn't ready — {orderProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        // Same "decision not yet made" rule for a staged defect with no description yet.
        if (createReady && stagedCreate is { Kind: StagedRecordKind.Defect } stagedDefect
            && stagedDefect.DefectProblem is { } defectProblem)
        {
            actionError = $"The staged defect isn't ready — {defectProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        // And for a staged inventory item with no product name yet.
        if (createReady && stagedCreate is { Kind: StagedRecordKind.Inventory } stagedInventory
            && stagedInventory.InventoryProblem is { } inventoryProblem)
        {
            actionError = $"The staged inventory item isn't ready — {inventoryProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        if (createReady && StagedTenderEnquiryProblem is { } enquiryProblem)
        {
            actionError = $"The staged tender enquiry isn't ready — {enquiryProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        // Same "decision not yet made" rule for a staged calendar event that isn't complete yet.
        if (createReady && StagedCalendarEventProblem is { } calendarProblem)
        {
            actionError = $"The staged calendar event isn't ready — {calendarProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        // And for a staged building control inspection.
        if (createReady && StagedBuildingControlInspectionProblem is { } inspectionProblem)
        {
            actionError = $"The staged inspection isn't ready — {inspectionProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        if (replying)
        {
            if (ParseRecipients(replyToField).Count == 0) { actionError = "Add a To recipient to the reply."; return; }
            if (string.IsNullOrWhiteSpace(replySubject)) { actionError = "Write a subject for the reply."; return; }
            // A reply alone triages the thread as Replied — pathway-less is fine (answering IS
            // dealing with it); choosing a tab in System Tags files it under that side as well.
        }

        var anchor = selected;
        var uploadParts = UploadPartsOf(replyAttachments);
        actionError = null;
        busy = true;
        try
        {
            var filed = false;

            // ---- "Use existing tags" answered Yes: resolve the thread's tag stems back to
            //      records FIRST (the same ResolveRecordTags behind the search chips), so a stem
            //      that no longer names anything stops the apply before anything else lands —
            //      the same every-tag-verified-before-anything-saves rule as the rest of the
            //      filing. The links themselves land with the picks below. ----
            IReadOnlyList<LinkableRecord> inheritedRecords = Array.Empty<LinkableRecord>();
            if (anchor is not null && inheritStems.Count > 0)
            {
                busyLabel = "Matching the thread's tags";
                inheritedRecords = await Queries.AskAsync(
                    new ResolveRecordTags(inheritStems.Select(TagLabel).ToList()), CancellationToken.None);
                if (inheritedRecords.Count == 0)
                {
                    actionError = "The thread's existing tags couldn't be matched to records — pick this email's records by hand instead.";
                    return;
                }
            }

            // ---- Document Triage: ticked attachments copy out FIRST, so the files are safely
            //      in the queue before anything else (a discard included) moves the email on.
            //      Never consumes the email — only the files are copied out; `filed` is
            //      deliberately not set. ----
            if (anchor is not null && stagedDocControlIds.Count > 0)
            {
                busyLabel = "Sending to Document Triage";
                await Commands.SendAsync(
                    new SendAttachmentsToDocumentControl(
                        anchor.Id, anchor.InternetMessageId,
                        stagedDocControlIds.ToList(), NullIfBlank(triageProjectId)),
                    CancellationToken.None);
                // One send per apply: clear the ticks (the server skips already-sent ids
                // regardless).
                stagedDocControlIds.Clear();
            }

            // ---- Section 2: to-dos (their command verifies every tag before saving) ----
            if (drafts.Count > 0)
            {
                busyLabel = "Creating to-dos";
                // No request link here: to-dos are their own concern, and linking the email to a
                // record — a request included — is the filing section's job.
                await Intake.CreateTodoItemsFromMessageAsync(new CreateTodoItemsFromMessage(
                    anchor.Id,
                    NullIfBlank(triageProjectId),
                    drafts,
                    LinkRequestId: null,
                    InternetMessageId: anchor.InternetMessageId,
                    Pathway: pathway is { } chosenForTodos ? PathwayLabel(chosenForTodos) : null,
                    Scope: scope));
                // One batch per apply: clear the rows so nothing can double-raise.
                createTodoRows = new List<TodoDraftRow> { new() };
                filed = true;
            }

            // ---- Record filing: every staged link applies, whatever picker is open ----
            if (anchor is not null && picks.Count > 0)
            {
                busyLabel = "Linking";
                foreach (var record in picks)
                {
                    // AllowCrossPathway: true — the pane choice IS the cross-filing decision
                    // (confirm retired 2026-08-28; true also keeps an older api from prompting).
                    await Intake.LinkMessageToRecordAsync(
                        anchor.Id, anchor.InternetMessageId, record.Type, record.RecordId,
                        pathway: CostCentrePathwayFor(record),
                        allowCrossPathway: true,
                        scope: scope);
                    filed = true;
                }
            }
            // ---- The thread's existing tags, answered Yes above: each resolved record links
            //      exactly like a picked one. Records the triager ALSO picked by hand are
            //      skipped — one link per record per apply. allowCrossPathway is true outright:
            //      these tags are already on the thread, so re-filing this reply under them is
            //      never a new cross-pathway decision. ----
            if (anchor is not null && inheritedRecords.Count > 0)
            {
                busyLabel = "Linking to the thread's tags";
                foreach (var record in inheritedRecords)
                {
                    if (picks.Any(pick => pick.Type == record.Type
                        && string.Equals(pick.RecordId, record.RecordId, StringComparison.Ordinal)))
                        continue;
                    await Intake.LinkMessageToRecordAsync(
                        anchor.Id, anchor.InternetMessageId, record.Type, record.RecordId,
                        pathway: CostCentrePathwayFor(record),
                        allowCrossPathway: true,
                        scope: scope);
                    filed = true;
                }
            }
            // A Relevant Event answered Yes: link the thread to the project's programme bucket — the
            // record id IS the project id (one bucket per project, SchedulingLinkProvider).
            // Scheduling is a Client-side record, so on a non-client thread this cross-files the
            // thread — allowed without a confirm, like the picks above.
            if (relevantEvent)
            {
                busyLabel = "Tagging relevant event";
                await Intake.LinkMessageToRecordAsync(
                    anchor.Id, anchor.InternetMessageId, RecordType.Scheduling, triageProjectId,
                    pathway: null,
                    allowCrossPathway: true,
                    scope: scope);
                filed = true;
            }
            if (createReady && stagedCreate is { } staged)
            {
                var created = await RaiseStagedRecordAsync(staged, anchor!, scope);
                // One create per apply: clear it so nothing can double-create.
                stagedCreate = null;
                createdNowRecords.Add(created.Record);
                filed = true;
                if (created.UploadError is not null)
                {
                    // The order exists and the email is tagged to it — never re-raise it. Stop
                    // here (before any reply sends) so the failure is seen; the email stays
                    // selected and the files can be re-added from the order's PO page.
                    actionError = created.UploadError;
                    return;
                }
            }
            // ---- System actions lined up in the Actions pane — run once the filing above has
            //      landed, each removed as it succeeds so a failed one can be retried without
            //      re-running its predecessors. A failure stops the apply with its reason. ----
            foreach (var stagedAction in stagedSystemActions.ToList())
            {
                busyLabel = $"System action: {SystemActionKinds.Label(stagedAction.Kind)}";
                await stagedAction.ExecuteAsync();
                stagedSystemActions.Remove(stagedAction);
            }

            if (discarding)
            {
                // "File it as nothing": tag the thread discarded — restorable from the Tagged tab.
                // Runs after the to-dos so "capture the follow-ups, then bin the email" works.
                busyLabel = "Discarding";
                await Intake.DiscardMessageAsync(anchor.Id, anchor.InternetMessageId);
                filed = true;
            }

            // ---- The Outbox: replies lined up against OLDER emails. Each anchor email is first
            //      tagged to the triage's record picks (one triage decision covers every email
            //      answered — decision 2026-08-12), then the reply sends; the server files the
            //      sent copy by the anchor's tags, the fresh ones included, because the links
            //      land before the send. MessageOnly spread: the reply answers THAT email; the
            //      selected email's thread decision doesn't reach into other conversations. Each
            //      entry is removed as it completes, so a failure stops the apply with the
            //      already-sent replies never re-sent. ----
            var outboxSent = 0;
            foreach (var lined in queuedReplies.ToList())
            {
                foreach (var record in picks)
                {
                    busyLabel = "Tagging lined-up replies";
                    await Intake.LinkMessageToRecordAsync(
                        lined.MessageId, lined.InternetMessageId, record.Type, record.RecordId,
                        pathway: CostCentrePathwayFor(record),
                        allowCrossPathway: true,
                        scope: LinkThreadScope.MessageOnly);
                }
                busyLabel = saveAsDraftOnly ? "Saving lined-up drafts" : "Sending lined-up emails";
                // MarkThreadHandled off: the anchor is an already-triaged email — its record tags
                // say more than Replied would, and it isn't sitting in the queue to clear. A
                // lined-up FORWARD routes through Graph's createForward server-side (Forward).
                var linedCommand = new SendMailboxEmail(
                    ReplyToMessageId: lined.MessageId,
                    ReplyToInternetMessageId: lined.InternetMessageId,
                    To: MailCompose.ParseRecipients(lined.ToField),
                    Cc: MailCompose.ParseRecipients(lined.CcField),
                    Bcc: MailCompose.ParseRecipients(lined.BccField),
                    Subject: lined.Subject.Trim(),
                    Body: lined.Body,
                    BodyIsHtml: true,
                    Attachments: lined.Attachments.Select(a => a.ToRef()).ToList(),
                    SaveAsDraftOnly: saveAsDraftOnly,
                    Pathway: null,
                    MarkThreadHandled: false,
                    Forward: lined.IsForward);
                await Intake.SendComposedEmailAsync(linedCommand, MailCompose.UploadPartsOf(lined.Attachments));
                queuedReplies.Remove(lined);
                outboxSent++;
            }
            if (outboxSent > 0)
                outboxNote = saveAsDraftOnly
                    ? $"{outboxSent} lined-up {(outboxSent == 1 ? "email was" : "emails were")} saved to the mailbox's Drafts — review and send from Outlook."
                    : $"{outboxSent} lined-up {(outboxSent == 1 ? "email was" : "emails were")} sent from the projects mailbox{(picks.Count > 0 ? ", each email tagged to the picked records" : "")}.";

            // ---- Section 1: the reply (or forward) — last, so nothing above can be lost to a
            //      send failure. When a filing already dealt with the thread its record tag says
            //      more than Replied, so the stamp is skipped — and a FORWARD never stamps: it
            //      passes the email on rather than answering it, so the email stays queued
            //      unless a filing above dealt with it. ----
            if (replying)
            {
                busyLabel = saveAsDraftOnly ? "Saving draft" : (replyIsForward ? "Sending forward" : "Sending reply");
                var command = new SendMailboxEmail(
                    ReplyToMessageId: anchor.Id,
                    ReplyToInternetMessageId: anchor.InternetMessageId,
                    To: ParseRecipients(replyToField),
                    Cc: ParseRecipients(replyCcField),
                    Bcc: ParseRecipients(replyBccField),
                    Subject: replySubject.Trim(),
                    Body: replyBody,
                    BodyIsHtml: true,
                    Attachments: replyAttachments.Select(a => a.ToRef()).ToList(),
                    SaveAsDraftOnly: saveAsDraftOnly,
                    Pathway: pathway?.ToString(),
                    MarkThreadHandled: !filed && !replyIsForward,
                    Forward: replyIsForward);
                composeOutcome = await Intake.SendComposedEmailAsync(command, uploadParts);
                replyBody = "";
                replyOpen = false;
                replyIsForward = false;
                replyAttachments = Array.Empty<ComposeDraftAttachment>();
            }

            // Applied in full: refresh the queue in place — the triager stays on the page they
            // were working — and clear the selection (the email has left it). The Triage tab
            // hands back to the queue list, ready for the next email.
            await Task.WhenAll(ReloadQueueInPlaceAsync(), LoadRecentTriageAsync());
            selected = null;
            detail = null;
            detailLoading = false;
            discardArmed = false;
            stagedCreate = null;
            createdNowRecords.Clear();
            relevantEventStaged = null;
            triageEntireThread = null;
            useThreadTags = null;
            pickedRecords.Clear();
            stagedSystemActions.Clear();
            ReturnWorkspaceToQueue();
        }
        catch (CommandFailedException ex)
        {
            actionError = ex.Message;
        }
        catch
        {
            actionError = "That didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    /// <summary>What one staged-create execution produced: the created-record chip for the pane,
    /// and — for a work order whose picked files failed to upload — the error that stops the
    /// caller (the order exists and the email is tagged; the files are re-added from the order's
    /// PO page).</summary>
    private sealed record StagedCreateOutcome(CreatedNowRecord Record, string? UploadError);

    /// <summary>
    /// Raises the staged record and tags the email to it — the create-on-apply body, shared
    /// verbatim by Apply and by System Actions' "Create now". Every command goes out with
    /// AllowCrossPathway: true — the pane choice IS the cross-filing decision (the confirm was
    /// retired 2026-08-28), and true keeps an older api from prompting.
    /// </summary>
    private async Task<StagedCreateOutcome> RaiseStagedRecordAsync(
        StagedRecordCreate staged, MailboxMessage anchor, LinkThreadScope scope)
    {
        if (staged.Kind == StagedRecordKind.BidPackage)
        {
            busyLabel = "Creating bid package";
            var package = await Intake.CreateBidPackageFromMessageAsync(new CreateBidPackageFromMessage(
                anchor.Id, triageProjectId, staged.Title.Trim(), staged.Trade?.Trim() ?? "",
                InternetMessageId: anchor.InternetMessageId,
                Scope: scope,
                AllowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(package.Reference, "bid package", staged.Title.Trim()), null);
        }
        if (staged.Kind == StagedRecordKind.WorkOrder)
        {
            // The full manual-order surface staged in System Actions, raised through the
            // same rules as the Work Orders tab (numbering, draft semantics, cost-code
            // master guard) with the email tagged to the new order.
            busyLabel = "Raising work order";
            var orderLines = staged.EnteredLines
                .Where(line => line.CostCode != "" && line.Amount is { } amount && amount != 0m)
                .Select(line => new ManualWorkOrderLine(
                    line.CostCode, line.Title.Trim(), line.Amount!.Value, line.Description.Trim()))
                .ToList();
            var raisedOrder = await Intake.CreateWorkOrderFromMessageAsync(new CreateWorkOrderFromMessage(
                anchor.Id, triageProjectId, staged.SubcontractorId,
                staged.Title.Trim(), staged.Scope.Trim(), orderLines,
                ProgrammeStart: AsUtcDate(staged.ProgrammeStart),
                TargetCompletion: AsUtcDate(staged.TargetCompletion),
                ProgrammeNotes: staged.ProgrammeNotes.Trim(),
                SaveAsDraft: staged.SaveAsDraft,
                DepositRequired: staged.DepositRequired,
                DepositPercent: staged.DepositRequired
                    ? StagedRecordCreate.ParseDecimal(staged.DepositPercentText)
                    : null,
                InternetMessageId: anchor.InternetMessageId,
                // Named LinkScope on this command only: the order's own Scope (works text)
                // already owns the name — see CreateWorkOrderFromMessage.
                LinkScope: scope,
                // Ticked email attachments — copied onto the order server-side (record
                // keeping only; never sent to the supplier).
                AttachmentIds: staged.EmailAttachmentIds.Count > 0 ? staged.EmailAttachmentIds.ToList() : null,
                AllowCrossPathway: true));

            // The email the modal's warning promised: a released (non-draft) order sends
            // its purchase order to the supplier there and then. Non-fatal by design —
            // the order is raised and the email tagged to it either way; the note (shown
            // where the cleared selection was) says what happened.
            if (!staged.SaveAsDraft)
            {
                busyLabel = "Emailing purchase order";
                (poEmailNote, poEmailNoteIsSuccess) = await TrySendWorkOrderPoEmailAsync(raisedOrder, orderLines);
            }

            var orderRecord = new CreatedNowRecord(
                raisedOrder.Reference,
                staged.SaveAsDraft ? "draft work order" : "work order",
                staged.Title.Trim());

            // Files picked from this computer land straight after the order exists —
            // multipart to the order's attachment endpoint. Record keeping only: never
            // part of the purchase-order email above.
            if (staged.UploadFiles.Count > 0)
            {
                busyLabel = "Uploading attachments";
                try
                {
                    await WorkOrderAttachments.UploadFilesAsync(raisedOrder.WorkOrderId, staged.UploadFiles.ToList());
                }
                catch (Exception ex)
                {
                    var fileCount = staged.UploadFiles.Count;
                    return new StagedCreateOutcome(orderRecord,
                        $"{raisedOrder.Reference} was raised and this email tagged to it, but the picked "
                        + $"file{(fileCount == 1 ? "" : "s")} couldn't be uploaded — add "
                        + $"{(fileCount == 1 ? "it" : "them")} again from the order's PO page. ({ex.Message})");
                }
            }
            return new StagedCreateOutcome(orderRecord, null);
        }
        if (staged.Kind == StagedRecordKind.Defect)
        {
            // The defect staged in System Actions, raised through the same rules as a manual
            // defect (numbering, Open status) with the email tagged to it.
            busyLabel = "Raising defect";
            var defect = await Intake.CreateDefectFromMessageAsync(new Jewel.JPMS.Contracts.Closeout.CreateDefectFromMessage(
                anchor.Id, triageProjectId,
                staged.Description.Trim(),
                staged.DefectLocation.Trim(),
                staged.DefectAssignedTo.Trim(),
                InternetMessageId: anchor.InternetMessageId,
                Scope: scope,
                AllowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(defect.Reference, "defect", staged.DisplayTitle), null);
        }

        if (staged.Kind == StagedRecordKind.Inventory)
        {
            // The inventory item staged in the Supplier pane's Actions, added through the same
            // rules as one added on the project's Inventory tab (INV numbering) with the
            // supplier's email tagged to it.
            busyLabel = "Adding inventory item";
            var item = await Intake.CreateInventoryItemFromMessageAsync(new Jewel.JPMS.Contracts.Inventory.CreateInventoryItemFromMessage(
                anchor.Id, triageProjectId,
                staged.Title.Trim(),
                staged.Description.Trim(),
                staged.InventoryLocation.Trim(),
                staged.InventoryLocationDetails.Trim(),
                InternetMessageId: anchor.InternetMessageId,
                Scope: scope,
                AllowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(item.Reference, "inventory item", staged.Title.Trim()), null);
        }

        if (staged.Kind == StagedRecordKind.CalendarEvent)
        {
            // The calendar event staged in System Actions, raised through the same rules as one
            // added on the Calendar tab (CAL numbering, midnight-UTC date) with the email tagged
            // to it.
            busyLabel = "Raising calendar event";
            var calendarEvent = await Intake.CreateCalendarEventFromMessageAsync(
                staged.CalendarEvent.ToCommand(anchor.Id, anchor.InternetMessageId, triageProjectId, scope, allowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(calendarEvent.Reference, "calendar event", calendarEvent.Title), null);
        }

        if (staged.Kind == StagedRecordKind.BuildingControlInspection)
        {
            // The inspection staged in System Actions, raised through the same rules as one added
            // on the Building Control tab (BCI numbering, foot of the running order, Booked when
            // dated) with the inspector's email tagged to it. Requires the project's case; the
            // server's refusal lands in the red bar with its own wording.
            busyLabel = "Raising building control inspection";
            var inspection = await Intake.CreateBuildingControlInspectionFromMessageAsync(
                staged.BuildingControlInspection.ToCommand(anchor.Id, anchor.InternetMessageId, triageProjectId, scope, allowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(inspection.Reference, "building control inspection", inspection.StageName), null);
        }

        if (staged.Kind == StagedRecordKind.TenderEnquiry)
            return await LogStagedTenderEnquiryAsync(staged, anchor, scope);

        busyLabel = staged.RequestKind == RequestType.Rfi ? "Raising RFI" : "Creating request";
        var request = await Intake.CreateRequestFromMessageAsync(new CreateRequestFromMessage(
            anchor.Id, triageProjectId, staged.RequestKind, "", staged.Title.Trim(),
            staged.Description?.Trim() ?? "",
            DrawingRef: NullIfBlank(staged.DrawingRef),
            ResponseDue: ParseDate(staged.ResponseDue),
            InternetMessageId: anchor.InternetMessageId,
            AddToProgramme: staged.AddToProgramme,
            Scope: scope,
            AllowCrossPathway: true));
        return new StagedCreateOutcome(
            new CreatedNowRecord(
                request.Reference,
                staged.RequestKind == RequestType.Rfi ? "RFI" : "request",
                staged.Title.Trim()),
            null);
    }

    /// <summary>
    /// The tender enquiry staged in System Actions, logged through LogTenderEnquiryFromMessage:
    /// its Lead project created when the job is new (the bar then points at that project, so the
    /// NEXT act on this email — a reply, a Create now follow-up — lands there; to-dos staged in
    /// the same apply have already been raised company-wide), the ticked files copied across,
    /// the email tagged to the enquiry.
    /// </summary>
    private async Task<StagedCreateOutcome> LogStagedTenderEnquiryAsync(
        StagedRecordCreate staged, MailboxMessage anchor, LinkThreadScope scope)
    {
        busyLabel = "Logging tender enquiry";
        var enquiry = await Intake.LogTenderEnquiryFromMessageAsync(
            staged.TenderEnquiry.ToCommand(anchor.Id, anchor.InternetMessageId, triageProjectId, scope, allowCrossPathway: true));
        if (staged.TenderEnquiry.CreatesNewProject)
        {
            await LoadProjectsAsync();
            triageProjectId = enquiry.ProjectId;
        }
        return new StagedCreateOutcome(
            new CreatedNowRecord(enquiry.Reference, "tender enquiry", enquiry.Title), null);
    }

    /// <summary>
    /// System Actions' "Create now": raises the staged record IMMEDIATELY — same body as the
    /// apply's create (record raised, email tagged to it, PO email for a released order) — so the
    /// new record exists and can be worked with (linked elsewhere, named in the reply, picked in
    /// the tag pickers) before the rest of the triage lands. The chip in the pane swaps from
    /// "will raise" to the raised reference; Apply then lands whatever else is staged, with
    /// nothing left to double-create.
    /// </summary>
    private async Task DoCreateStagedNow()
    {
        if (busy) return;
        if (selected is not { } anchor || stagedCreate is not { } staged) return;
        if (!StagedCreateReady)
        {
            actionError = staged.Kind switch
            {
                StagedRecordKind.Defect => "Describe the defect first — then Create now.",
                StagedRecordKind.Inventory => "Name the product first — then Create now.",
                _ => "Give the staged record a title first — then Create now."
            };
            return;
        }
        if (string.IsNullOrWhiteSpace(triageProjectId) && !StagedCreatesOwnProject)
        {
            actionError = "To create the record now, set the email's Project in the bar above first.";
            return;
        }
        // The same "decision not yet made" gates as Apply, for the decisions this act consumes.
        if (StagedTenderEnquiryProblem is { } enquiryProblem)
        {
            actionError = $"The staged tender enquiry isn't ready — {enquiryProblem}";
            return;
        }
        if (StagedCalendarEventProblem is { } calendarNowProblem)
        {
            actionError = $"The staged calendar event isn't ready — {calendarNowProblem}";
            return;
        }
        if (StagedBuildingControlInspectionProblem is { } inspectionNowProblem)
        {
            actionError = $"The staged inspection isn't ready — {inspectionNowProblem}";
            return;
        }
        if (staged is { Kind: StagedRecordKind.WorkOrder } stagedOrder
            && stagedOrder.WorkOrderProblem is { } orderProblem)
        {
            actionError = $"The staged work order isn't ready — {orderProblem}";
            return;
        }
        if (staged is { Kind: StagedRecordKind.Defect } stagedDefect
            && stagedDefect.DefectProblem is { } defectProblem)
        {
            actionError = $"The staged defect isn't ready — {defectProblem}";
            return;
        }
        if (staged is { Kind: StagedRecordKind.Inventory } stagedInventory
            && stagedInventory.InventoryProblem is { } inventoryProblem)
        {
            actionError = $"The staged inventory item isn't ready — {inventoryProblem}";
            return;
        }
        // Creating now tags the email to the new record, so the thread-spread decision must be
        // made — the Relevant Event answer can wait for Apply, which is what consumes it.
        if (triageEntireThread is null)
        {
            actionError = "Answer Entire thread — Yes or No — so Create now knows how far the email tag spreads.";
            return;
        }

        var scope = triageEntireThread == true ? LinkThreadScope.EntireThread : LinkThreadScope.MessageOnly;
        actionError = null;
        busy = true;
        try
        {
            var created = await RaiseStagedRecordAsync(staged, anchor, scope);
            stagedCreate = null;
            createdNowRecords.Add(created.Record);
            if (created.UploadError is not null)
            {
                actionError = created.UploadError;
                return;
            }
            // The record exists and this email is tagged to it — surface it in Recently
            // processed and show the tag on the email itself. The email stays selected: the
            // rest of the triage (reply, tags, to-dos, the two Yes/No answers) still lands
            // with Apply.
            await Task.WhenAll(LoadRecentTriageAsync(), RefreshSelectedTagsAsync(anchor));
        }
        catch (CommandFailedException ex)
        {
            actionError = ex.Message;
        }
        catch
        {
            actionError = "That didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    /// <summary>Sends the purchase-order email a released (non-draft) work order promised —
    /// the same covering email every other route sends (WorkOrderPoEmail). Never throws: the
    /// order is already raised, so the outcome is a note, not an error.</summary>
    private async Task<(string Note, bool Sent)> TrySendWorkOrderPoEmailAsync(
        WorkOrder order, IReadOnlyList<ManualWorkOrderLine> orderLines)
    {
        var supplier = (Subcontractors.Current ?? Array.Empty<Subcontractor>()).FirstOrDefault(sub =>
            string.Equals(sub.SubcontractorId, order.SubcontractorId, StringComparison.OrdinalIgnoreCase));
        if (supplier is null || string.IsNullOrWhiteSpace(supplier.ContactEmail))
            return ($"{order.Reference} was raised, but the supplier has no email address in the directory "
                + "so the purchase order wasn't emailed — add one, then send it from the order's PO page.", false);

        var projectName = Projects.Find(triageProjectId)?.Name ?? "";
        var emailLines = orderLines
            .Select(line => new WorkOrderPoEmail.Line(line.Title, 1m, "item", line.Amount))
            .ToList();
        try
        {
            var outcome = await Commands.SendAsync(new SendWorkOrderPoEmail(
                order.WorkOrderId,
                WorkOrderPoEmail.Subject(order, string.IsNullOrWhiteSpace(projectName) ? triageProjectId : projectName),
                WorkOrderPoEmail.Body(order, supplier.CompanyName, emailLines, projectName, Nav.BaseUri)),
                CancellationToken.None);
            return outcome.Sent
                ? ($"{order.Reference} was raised and the purchase order was emailed to {outcome.RecipientEmail}.", true)
                : ($"{order.Reference} was raised. {outcome.FailureNote}", false);
        }
        catch (CommandFailedException ex)
        {
            return ($"{order.Reference} was raised, but the purchase-order email couldn't be sent: "
                + $"{ex.Message} You can send it from the order's PO page.", false);
        }
        catch
        {
            return ($"{order.Reference} was raised, but the purchase-order email couldn't be sent "
                + "— you can send it from the order's PO page.", false);
        }
    }

    // Each edit also marks the envelope as the user's (2026-08-28): the reply-all prefill rides
    // in on the detail fetch, and an address or subject typed BEFORE that slow fetch lands must
    // never be overwritten by it — first touch takes ownership, the late prefill backs off.
    private void OnReplyToInput(ChangeEventArgs e) { replyToField = e.Value?.ToString() ?? ""; replyEnvelopePrefilled = true; }
    private void OnReplyCcInput(ChangeEventArgs e) { replyCcField = e.Value?.ToString() ?? ""; replyEnvelopePrefilled = true; }
    private void OnReplyBccInput(ChangeEventArgs e) { replyBccField = e.Value?.ToString() ?? ""; replyEnvelopePrefilled = true; }
    private void OnReplySubjectInput(ChangeEventArgs e) { replySubject = e.Value?.ToString() ?? ""; replyEnvelopePrefilled = true; }

    // ---- New email (fresh outbound thread from the projects mailbox) ----

    // Clears the compose form and hands its window back to whatever it showed before — pressed
    // as Cancel, and called after a successful send so the outcome banner is what remains.
    private void CloseNewEmail()
    {
        if (newEmailBusy) return;
        workspace.Close(PanelKind.Compose);
        newEmailError = null;
        newEmailTo = newEmailCc = newEmailBcc = newEmailSubject = newEmailBody = "";
        newEmailAttachments = Array.Empty<ComposeDraftAttachment>();
        newEmailFile = false;
        newEmailProjectId = "";
        newEmailRecordType = RecordType.Request;
        newEmailRecordId = "";
        newEmailRecords = Array.Empty<LinkableRecord>();
    }

    private bool NewEmailIsSendable =>
        ParseRecipients(newEmailTo).Count > 0
        && !string.IsNullOrWhiteSpace(newEmailSubject)
        && HtmlHasContent(newEmailBody)
        && (!newEmailFile || (!string.IsNullOrEmpty(newEmailProjectId) && !string.IsNullOrEmpty(newEmailRecordId)));

    private void OnNewEmailToInput(ChangeEventArgs e) => newEmailTo = e.Value?.ToString() ?? "";
    private void OnNewEmailCcInput(ChangeEventArgs e) => newEmailCc = e.Value?.ToString() ?? "";
    private void OnNewEmailBccInput(ChangeEventArgs e) => newEmailBcc = e.Value?.ToString() ?? "";
    private void OnNewEmailSubjectInput(ChangeEventArgs e) => newEmailSubject = e.Value?.ToString() ?? "";

    private void OnNewEmailBodyChanged(string html) => newEmailBody = html;
    private void OnNewEmailAttachmentsChanged(IReadOnlyList<ComposeDraftAttachment> attachments) =>
        newEmailAttachments = attachments;

    private void OnNewEmailFileToggled(ChangeEventArgs e)
    {
        newEmailFile = e.Value is true;
        if (!newEmailFile) { newEmailRecordId = ""; }
    }

    private async Task OnNewEmailProjectChanged(ChangeEventArgs e)
    {
        newEmailProjectId = e.Value?.ToString() ?? "";
        newEmailRecordId = "";
        await LoadNewEmailRecordsAsync();
    }

    private async Task OnNewEmailRecordTypeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var t)) newEmailRecordType = (RecordType)t;
        newEmailRecordId = "";
        await LoadNewEmailRecordsAsync();
    }

    private void OnNewEmailRecordChanged(ChangeEventArgs e) => newEmailRecordId = e.Value?.ToString() ?? "";

    private async Task LoadNewEmailRecordsAsync()
    {
        newEmailRecords = Array.Empty<LinkableRecord>();
        if (string.IsNullOrEmpty(newEmailProjectId)) return;
        newEmailRecordsLoading = true;
        try { newEmailRecords = await Intake.ListLinkableRecordsAsync(newEmailProjectId, newEmailRecordType); }
        catch { newEmailError = "Couldn't load the records for that project. Please try again."; }
        finally { newEmailRecordsLoading = false; }
    }

    private async Task DoSendNewEmail(bool saveAsDraftOnly)
    {
        if (newEmailBusy) return;
        var to = ParseRecipients(newEmailTo);
        if (to.Count == 0) { newEmailError = "Add a To recipient."; return; }
        if (string.IsNullOrWhiteSpace(newEmailSubject)) { newEmailError = "Write a subject."; return; }
        if (!HtmlHasContent(newEmailBody)) { newEmailError = "Write the email first."; return; }

        var command = new SendMailboxEmail(
            ReplyToMessageId: null,
            ReplyToInternetMessageId: null,
            To: to,
            Cc: ParseRecipients(newEmailCc),
            Bcc: ParseRecipients(newEmailBcc),
            Subject: newEmailSubject.Trim(),
            Body: newEmailBody,
            BodyIsHtml: true,
            Attachments: newEmailAttachments.Select(a => a.ToRef()).ToList(),
            SaveAsDraftOnly: saveAsDraftOnly,
            Pathway: null,
            MarkThreadHandled: false,
            LinkRecordType: newEmailFile && !string.IsNullOrEmpty(newEmailRecordId) ? newEmailRecordType : null,
            LinkRecordId: newEmailFile && !string.IsNullOrEmpty(newEmailRecordId) ? newEmailRecordId : null,
            ProjectId: newEmailFile && !string.IsNullOrEmpty(newEmailProjectId) ? newEmailProjectId : null);
        var uploadParts = UploadPartsOf(newEmailAttachments);

        newEmailError = null;
        newEmailBusy = true;
        try
        {
            composeOutcome = await Intake.SendComposedEmailAsync(command, uploadParts);
            newEmailBusy = false;
            CloseNewEmail();
        }
        catch (CommandFailedException ex)
        {
            newEmailError = ex.Message;
        }
        catch
        {
            newEmailError = "The send didn't complete. Please try again.";
        }
        finally { newEmailBusy = false; }
    }

    private void OnReplyBodyInput(ChangeEventArgs e) => replyBody = e.Value?.ToString() ?? "";

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

    // ---- Preview an attachment without leaving triage ----
    // Same previewable set as the drawing viewer: PDFs (the in-app viewer) and images; everything
    // else gets a Download link only. Bytes are proxied through the API on demand
    // (mailbox/message/attachment) — nothing is stored in JPMS by previewing. The document opens
    // in the Preview pane on the window OPPOSITE the email, the same route as a record's
    // documents, so email and attachment read side by side. The URLs are baked at click time —
    // the preview outlives the selection that opened it.

    private static bool IsPreviewable(IntakeAttachment attachment)
    {
        var type = attachment.ContentType ?? "";
        return type.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            || type.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    // Ids go in the query string, never the path — Graph ids don't survive a URL path segment.
    private string AttachmentUrl(IntakeAttachment attachment, bool inline) =>
        $"/api/mailbox/message/attachment?id={Uri.EscapeDataString(selected?.Id ?? "")}"
        + $"&aid={Uri.EscapeDataString(attachment.Id)}{(inline ? "&inline=1" : "")}";

    private void OpenEmailAttachmentPreview(IntakeAttachment attachment)
    {
        var isPdf = (attachment.ContentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase);
        workspace.OpenPreview(
            new PreviewRequest(attachment.Name, AttachmentUrl(attachment, inline: true),
                AttachmentUrl(attachment, inline: false), isPdf),
            anchor: PanelKind.Email);
    }

    // ---- Send attachments to Document Triage ----
    // Ticked per attachment on the open email and staged like every other triage draft — the
    // email's Apply copies the files mailbox → Document Triage server-side. Like the
    // save-to-drawings form this replaced (2026-08-12), it does NOT consume the email: the
    // message keeps its place in triage — only the files are copied out. Choosing each file's
    // DESTINATION happens in Document Triage itself, but the PROJECT is decided here, where
    // the email says which job it is: Apply requires one while attachments are ticked
    // (decision 2026-08-28 — a projectless file in the queue is as good as discarded).

    private readonly List<string> stagedDocControlIds = new();

    private void ToggleDocControl(string attachmentId, bool ticked)
    {
        if (ticked)
        {
            if (!stagedDocControlIds.Contains(attachmentId)) stagedDocControlIds.Add(attachmentId);
        }
        else
        {
            stagedDocControlIds.Remove(attachmentId);
        }
    }

    // Runs a Queue-tab action (assign / create / discard), then refreshes the inbox and clears the
    // selection — the message has left the Inbox, so it drops out of the live read. `label` captions
    // the detail-pane spinner while the action is in flight.
    private async Task RunAction(string label, Func<Task> action)
    {
        actionError = null;
        try
        {
            busyLabel = label;
            busy = true;
            await action();
            // The queue and the recently-triaged panel move together: the action that consumed
            // this email is the newest row of the panel. The reload stays on the current page
            // (in-place) so emails the triager skipped don't come round again after every action.
            await Task.WhenAll(ReloadQueueInPlaceAsync(), LoadRecentTriageAsync());
            selected = null;
            detail = null;
            detailLoading = false;
            discardArmed = false;
            stagedCreate = null;
            relevantEventStaged = null;
            triageEntireThread = null;
            useThreadTags = null;
            pickedRecords.Clear();
            ReturnWorkspaceToQueue();
        }
        catch (CommandFailedException ex)
        {
            // e.g. the reference is already in use on this project.
            actionError = ex.Message;
        }
        catch
        {
            actionError = "That action didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    // Runs a Tagged-tab action (add a tag, remove a tag), then refreshes the tagged list and clears the
    // selection — the email's tag set has changed, so re-read it live.
    private async Task RunTaggedAction(string label, Func<Task> action)
    {
        actionError = null;
        try
        {
            busyLabel = label;
            busy = true;
            await action();
            await ReloadTaggedInPlaceAsync();
            selected = null;
            detail = null;
            detailLoading = false;
            ResetLinkState();
            ReturnWorkspaceToQueue();
        }
        catch
        {
            actionError = "That action didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    // Add another workflow tag by linking this already-tagged email to a second record (so it feeds
    // more than one record). Reuses the same generic link command as the queue's "Link to existing",
    // but NOT RunTaggedAction — a link failure belongs next to the picker, not in the toast. Any
    // crossing — the former client wall included (removed 2026-08-21) — simply files the thread
    // under both: AllowCrossPathway: true, since the picker's own heads-up already says where the
    // link files the thread (the confirm was retired 2026-08-28).
    private async Task DoAddTagLink()
    {
        if (selected is null || busy || string.IsNullOrWhiteSpace(linkRecordId)) return;
        // The type to link as is the picked RECORD's own type, not the dropdown's (the Scheduling
        // picker lists NOD/EOT/LAD claims documents alongside the bucket — see DoApplyAll).
        var record = linkRecords.FirstOrDefault(r => r.RecordId == linkRecordId);
        var recordType = record?.Type ?? linkRecordType;
        actionError = null;
        try
        {
            busyLabel = "Linking";
            busy = true;
            await Intake.LinkMessageToRecordAsync(
                selected.Id, selected.InternetMessageId, recordType, linkRecordId,
                pathway: record is null ? null : CostCentrePathwayFor(record),
                allowCrossPathway: true);
            await ReloadTaggedInPlaceAsync();
            selected = null;
            detail = null;
            detailLoading = false;
            ResetLinkState();
            ReturnWorkspaceToQueue();
        }
        catch (CommandFailedException ex)
        {
            actionError = ex.Message;
        }
        catch
        {
            actionError = "That action didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    private async Task DoRemoveTag(string tag)
    {
        if (selected is null || busy) return;
        await RunTaggedAction("Removing tag", async () => await Intake.RemoveTagFromMessageAsync(selected.Id, selected.InternetMessageId, tag));
    }

    private void OnTaggedRecordChanged(ChangeEventArgs e) => linkRecordId = e.Value?.ToString() ?? "";

    // Display label for a workflow tag chip: drop the "JPMS/" prefix (e.g. "JPMS/RFI-001" -> "RFI-001").
    private static string TagLabel(string tag) =>
        tag.StartsWith("JPMS/", StringComparison.OrdinalIgnoreCase) ? tag["JPMS/".Length..] : tag;

    // Every project the signed-in user can see, completed ones included. Use this for LOOKING A
    // PROJECT UP BY ID (a stored id can point at a completed project whatever the toggle says);
    // use ProjectOptionsFor for anything a user picks from.
    private IReadOnlyList<Project> AllProjects =>
        Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>();

    // Completed projects are hidden from every picker on this page by default: triage routes live
    // Completed projects follow the per-user ProjectStageFilter toggle (the same one the side-nav
    // switcher uses — decision 2026-08-03) rather than a page-local checkbox: one preference,
    // honoured everywhere. The picker keeps an already-chosen completed project visible so the
    // bound <select> never points at a missing option.
    private IReadOnlyList<Project> ProjectOptionsFor(string? selectedProjectId) =>
        AllProjects
            .Where(project =>
                StageFilter.IncludeCompleted
                || project.Stage != ProjectStage.Completed
                || (!string.IsNullOrWhiteSpace(selectedProjectId)
                    && string.Equals(project.ProjectId, selectedProjectId, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    // Subtle text-link styling for the list-sort preference (deliberately not a button pair).
    private string SortLinkClass(bool newest) =>
        newestFirst == newest
            ? "text-content font-medium underline underline-offset-4 decoration-line-strong"
            : "text-content-subtle hover:text-content";

    // The loaded records for the chosen type + project (empty until both are chosen and the load runs).
    private IReadOnlyList<LinkableRecord> ProjectRecords() => linkRecords;

    // Records on the chosen project whose reference or title overlaps the email subject — surfaced
    // first so a duplicate record isn't created for something already being tracked. Type-agnostic.
    private List<LinkableRecord> DuplicateCandidates()
    {
        var subject = selected?.Subject ?? "";
        var tokens = Tokenise(subject);
        return ProjectRecords()
            .Select(r => (r, score: Overlap(r, subject, tokens)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Select(x => x.r)
            .ToList();
    }

    private static int Overlap(LinkableRecord record, string subject, HashSet<string> subjectTokens)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(record.Reference) &&
            subject.Contains(record.Reference, StringComparison.OrdinalIgnoreCase))
            score += 10;
        foreach (var token in Tokenise(record.Title))
            if (subjectTokens.Contains(token)) score++;
        return score;
    }

    private static HashSet<string> Tokenise(string text) =>
        text.Split(new[] { ' ', '\t', '\n', '\r', '-', '_', '.', ',', ':', ';', '[', ']', '(', ')', '/' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 3)
            .ToHashSet();

    private string RowClass(MailboxMessage item)
    {
        var baseClass = "w-full text-left rounded-lg border px-3 py-2 transition";
        return selected?.Id == item.Id
            ? $"{baseClass} border-accent bg-surface-raised"
            : $"{baseClass} border-line hover:border-line-strong hover:bg-surface-raised";
    }

    private string ViewTabClass(QueueView tab)
    {
        var baseClass = "px-3 py-2 text-sm font-medium border-b-2 -mb-px transition";
        return view == tab
            ? $"{baseClass} border-accent text-content"
            : $"{baseClass} border-transparent text-content-muted hover:text-content";
    }

    private static string DisplayFrom(MailboxMessage item) =>
        string.IsNullOrWhiteSpace(item.FromName) ? item.FromEmail : item.FromName;

    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string Date(DateTimeOffset value) => value.LocalDateTime.ToString("d MMM yyyy, HH:mm");

    // Outlook-style compact date for list rows: time alone today, "Yesterday 14:21", day name
    // within the week, then the date.
    // The thread's outbound legs — sent from the projects mailbox itself (address learned from the
    // detail read). Ordered oldest-first like the thread.
    private IReadOnlyList<MailboxMessage> SentReplies =>
        detail?.MailboxAddress is { Length: > 0 } mailbox
            ? thread.Where(m => m.FromEmail.Equals(mailbox, StringComparison.OrdinalIgnoreCase)).ToList()
            : Array.Empty<MailboxMessage>();

    // 1-based position of the open email within its thread (oldest first), for the tab strip label.
    private int ThreadPositionOfSelected
    {
        get
        {
            for (var i = 0; i < thread.Count; i++)
                if (thread[i].Id == selected?.Id) return i + 1;
            return thread.Count;
        }
    }

    // Thread tab chips: the open email filled, the rest quiet; the newest carries a dot.
    private static string ThreadTabClass(bool isCurrent, bool isLatest) =>
        "rounded-md px-2 py-0.5 text-xs border transition "
        + (isCurrent
            ? "bg-accent text-accent-ink border-accent font-semibold"
            : "border-line text-content-muted hover:text-content hover:border-line-strong");

    // Sits beside Apply in the triage bar's action row — armed it reads negative, so the state
    // is visible right where the button that would act on it lives.
    private string DiscardTabClass =>
        "rounded-lg px-3 py-1.5 text-sm border transition "
        + (discardArmed
            ? "border-negative text-negative bg-negative/10 font-medium"
            : "border-line text-content-subtle hover:text-negative hover:border-negative/50");

    private string ProjectLabelFor(string projectId) =>
        AllProjects.FirstOrDefault(project => project.ProjectId == projectId)?.Name ?? "the chosen project";

    private static string ListDate(DateTimeOffset value)
    {
        var local = value.LocalDateTime;
        var today = DateTime.Now.Date;
        if (local.Date == today) return local.ToString("HH:mm");
        if (local.Date == today.AddDays(-1)) return $"Yesterday {local:HH:mm}";
        if (local.Date > today.AddDays(-6)) return local.ToString("ddd HH:mm");
        return local.ToString("d MMM yyyy");
    }

    // The group header a list row falls under — Today / Yesterday / day names this week / month.
    private static string DateGroupLabel(DateTimeOffset value)
    {
        var local = value.LocalDateTime;
        var today = DateTime.Now.Date;
        if (local.Date == today) return "Today";
        if (local.Date == today.AddDays(-1)) return "Yesterday";
        if (local.Date > today.AddDays(-6)) return local.ToString("dddd");
        return local.ToString("MMMM yyyy");
    }

    // Graph's bodyPreview can open with boilerplate line breaks; the row preview wants the first
    // line with any content, whitespace collapsed.
    private static string FirstLineOf(string preview)
    {
        var line = preview.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0) ?? "";
        return System.Text.RegularExpressions.Regex.Replace(line, "\\s+", " ");
    }

    // Avatar initials from the sender's display name ("Lorraine Proud" → "LP"), falling back to
    // the first letter of the address.
    private static string SenderInitials(MailboxMessage item)
    {
        var name = string.IsNullOrWhiteSpace(item.FromName) ? item.FromEmail : item.FromName;
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var words = name.Split(new[] { ' ', '|', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => char.IsLetter(w[0]))
            .Take(2)
            .Select(w => char.ToUpperInvariant(w[0]))
            .ToArray();
        return words.Length == 0 ? char.ToUpperInvariant(name.Trim()[0]).ToString() : new string(words);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // The to-do rows' assignee chips hold each pick as its TodoAssigneePicker value — a role
    // ("3"), optionally pinned to a person ("3|jane@…"). An empty list means unassigned; the
    // server raises one item per assignee in the list.
    private static IReadOnlyList<TodoAssignee> ParseTodoAssignees(IEnumerable<string> values) =>
        values
            .Select(TodoAssigneePicker.Parse)
            .Where(assignee => assignee is not null)
            .Select(assignee => assignee!)
            .Distinct()
            .ToList();

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    // Date-only picker value ("yyyy-MM-dd") → a UTC date, matching how the manual work-order
    // modal and the other date pickers send dates — the purchase order prints a date, not a moment.
    private static DateTimeOffset? AsUtcDate(string value) =>
        DateTime.TryParse(value, out var parsed)
            ? new DateTimeOffset(DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc))
            : null;

    private void StageFilterChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        taggedSearchDebounce?.Cancel();
        taggedSearchDebounce?.Dispose();
        StageFilter.OnChange -= StageFilterChanged;
        RequestRegister.OnChange -= StateHasChanged;
        workspace.OnChange -= StateHasChanged;
    }
}
