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
