using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequests
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string? Kind { get; set; }

    private bool isLoaded;

    // ---- Manual RFI entry ----------------------------------------------------------------------
    // The RFI-locked raise dialog (attachments and all). Most RFIs are raised from an email in
    // the Control Centre; this is the way in for one with no email behind it, or a legacy
    // back-fill via the form's "Log a historical RFI" tick.

    private bool raiseDialogOpen;

    private void OpenRaiseDialog() => raiseDialogOpen = true;
    private void CloseRaiseDialog() => raiseDialogOpen = false;

    private void OnRaised(Request raised)
    {
        raiseDialogOpen = false;
        Nav.NavigateTo($"/projects/{ProjectId}/requests/view/{raised.RequestId}");
    }

    private IReadOnlyList<Request> AllRecords => RequestRegister.ForProject(ProjectId);
    private int OpenCount => AllRecords.Count(r => r.Status is not RequestStatus.Closed);
    private int OverdueCount => AllRecords.Count(r => r.IsOverdue);
    private int GeneralCount => AllRecords.Count(r => r.Kind == RequestType.General);
    private int RfiCount => AllRecords.Count(r => r.Kind == RequestType.Rfi);
    private int OverdueRfiCount => AllRecords.Count(r => r.Kind == RequestType.Rfi && r.IsOverdue);

    // ---- Bulk selection ----------------------------------------------------------------------
    // One selection set serves two bulk actions: email drafts (RFIs — a General request has no
    // official document to send yet) and pre-RFI merging (open General requests). Selection
    // survives filter/tab switches within the page, so someone can tick rows across views.

    private readonly HashSet<string> selectedIds = new();
    private bool preparingDrafts;
    private string? draftBatchError;
    private RequestEmailDraftBatch? draftBatch;

    // Mirrors PrepareRequestEmailDraftsAuthorisation server-side (directors, project managers,
    // site managers and architects; admins carry every role server-side).
    private bool CanDraftEmail => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager or Role.Architect);

    // Mirrors MergeRequestsAuthorisation server-side (admins, directors and project managers).
    private bool CanMerge => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager);

    // ---- In-row status changes (the chip's dropdown) --------------------------------------------

    // Mirrors the detail page's CanEditDetails (and UpdateRequestDetailsAuthorisation server-side):
    // project managers and administrators.
    private bool CanChangeStatus => Session.AvailableRoles.Any(role => role is Role.Admin or Role.ProjectManager);

    private string? statusBusyRequestId;
    private string? statusError;

    // Same routing as the detail page's status pill, adapted for one-click register use: Closed
    // closes as at today (the record's own close flow remains the place to backdate); everything
    // else applies directly, keeping the recorded response text.
    private async Task ChangeRequestStatus((Request Record, RequestStatus Status) change)
    {
        var (record, status) = change;
        if (statusBusyRequestId is not null || status == record.Status) return;
        statusError = null;
        try
        {
            statusBusyRequestId = record.RequestId;

            if (status == RequestStatus.Closed)
            {
                var closed = await RequestRegister.CloseAsync(record.RequestId, record.ProjectId, DateTimeOffset.Now);
                if (!closed)
                    statusError = $"{RowRef(record)} couldn't be closed — it no longer exists.";
                return;
            }

            var hasResponse = !string.IsNullOrWhiteSpace(record.ResponseText);
            await RequestRegister.UpdateAsync(new UpdateRequestDetails(
                record.RequestId,
                record.Reference,
                record.Title,
                record.Description,
                status,
                record.Value,
                record.ResponseText,
                hasResponse ? (record.RespondedByEmail ?? Auth.CurrentUser?.Email) : record.RespondedByEmail,
                record.ImpliesVariation,
                record.DrawingRef,
                record.ResponseDue,
                record.RelatedDrawingSpec,
                record.InternalNotes,
                record.ClientNotes));
        }
        catch (CommandFailedException ex)
        {
            statusError = $"{RowRef(record)}: {ex.Message}";
        }
        catch
        {
            statusError = $"Couldn't change the status of {RowRef(record)}. Please try again.";
        }
        finally
        {
            statusBusyRequestId = null;
        }
    }

    private static string RowRef(Request record) =>
        string.IsNullOrWhiteSpace(record.Reference) ? record.DisplayNumber : record.Reference;

    private static bool IsRfi(Request record) => record.Kind == RequestType.Rfi;

    // Open General requests that haven't already been merged away — the merge candidates.
    private static bool IsMergeableGeneral(Request record) =>
        record.Kind == RequestType.General
        && record.MergedIntoRequestId is null
        && record.Status is not RequestStatus.Closed;

    private bool IsSelectableRow(Request record) =>
        (CanDraftEmail && IsRfi(record)) || (CanMerge && IsMergeableGeneral(record));

    private List<Request> SelectedRfis =>
        AllRecords.Where(r => selectedIds.Contains(r.RequestId) && IsRfi(r)).ToList();

    private List<Request> SelectedGenerals =>
        AllRecords.Where(r => selectedIds.Contains(r.RequestId) && IsMergeableGeneral(r)).ToList();

    private void ToggleSelect(Request record)
    {
        if (!IsSelectableRow(record)) return;
        if (!selectedIds.Remove(record.RequestId)) selectedIds.Add(record.RequestId);
    }

    // The header checkbox acts on the selectable rows currently shown: ticking it adds them all,
    // unticking removes them — without touching selections made under other filters.
    private void ToggleSelectAll(bool select)
    {
        var visible = FilteredRecords.Where(IsSelectableRow).Select(r => r.RequestId).ToList();
        if (select) selectedIds.UnionWith(visible);
        else selectedIds.ExceptWith(visible);
    }

    private void ClearSelection()
    {
        selectedIds.Clear();
        draftBatch = null;
        draftBatchError = null;
        mergeSurvivorId = null;
        mergeError = null;
        mergeResult = null;
    }

    // ---- Pre-RFI merge -----------------------------------------------------------------------
    // Exactly two selected General requests can be combined: the chosen survivor keeps its
    // reference/title and absorbs the other's description, conversation, items and emails; the
    // other closes with a "merged into" audit link (visible under Closed, never counted as open).

    private string? mergeSurvivorId;
    private bool merging;
    private string? mergeError;
    private string? mergeResult;

    private string? SurvivorId =>
        SelectedGenerals.Any(r => r.RequestId == mergeSurvivorId)
            ? mergeSurvivorId
            : SelectedGenerals.FirstOrDefault()?.RequestId;

    private static string RefLabel(Request record) =>
        !string.IsNullOrWhiteSpace(record.Reference) ? record.Reference
        : record.DisplayNumber.Length > 0 ? record.DisplayNumber
        : "(no ref)";

    private async Task MergeSelected()
    {
        var generals = SelectedGenerals;
        if (merging || generals.Count != 2) return;

        var survivor = generals.FirstOrDefault(r => r.RequestId == SurvivorId) ?? generals[0];
        var mergedAway = generals.First(r => r.RequestId != survivor.RequestId);

        merging = true;
        mergeError = null;
        mergeResult = null;
        try
        {
            await RequestRegister.MergeAsync(survivor.RequestId, mergedAway.RequestId, ProjectId);
            mergeResult = $"{RefLabel(mergedAway)} merged into {RefLabel(survivor)} — its conversation, queries and emails now live there.";
            selectedIds.Remove(survivor.RequestId);
            selectedIds.Remove(mergedAway.RequestId);
            mergeSurvivorId = null;
        }
        catch (Exception ex)
        {
            mergeError = ex.Message;
        }
        finally
        {
            merging = false;
        }
    }

    private string LabelFor(RequestEmailDraftOutcome outcome) =>
        !string.IsNullOrWhiteSpace(outcome.Reference)
            ? outcome.Reference!
            : AllRecords.FirstOrDefault(r => r.RequestId == outcome.RequestId)?.Reference ?? outcome.RequestId;

    private async Task PrepareSelectedDrafts()
    {
        var rfiIds = SelectedRfis.Select(r => r.RequestId).ToList();
        if (preparingDrafts || rfiIds.Count == 0) return;
        preparingDrafts = true;
        draftBatch = null;
        draftBatchError = null;
        try
        {
            var batch = await RequestRegister.PrepareEmailDraftsAsync(rfiIds);
            draftBatch = batch;
            // Drafted RFIs come off the selection; failures stay ticked for a fix-and-retry.
            foreach (var outcome in batch.Outcomes.Where(o => o.Succeeded))
                selectedIds.Remove(outcome.RequestId);
            // Drafting moved each Open RFI to Awaiting Response server-side (manually set back to
            // Open if a send is cancelled) — revalidate the register so the table shows it.
            if (batch.Outcomes.Any(o => o.Succeeded))
                RequestRegister.Refresh(ProjectId);
        }
        catch (Exception ex)
        {
            draftBatchError = ex.Message;
        }
        finally
        {
            preparingDrafts = false;
        }
    }

    // ---- Document-type tabs --------------------------------------------------------------------

    private record Filter(string Slug, string Label, RequestType? Kind);

    // RFIs lead — they are what the view is for; the legacy General requests sit one tab behind
    // (the only way in, deliberately: requests are being sunset and nothing links them up front).
    // Variations moved to their own page (/projects/{id}/variations, split 2026-08-14).
    private static readonly Filter[] Filters =
    {
        new("rfis",    "RFIs",     RequestType.Rfi),
        new("general", "Requests", RequestType.General)
    };

    // RFIs are the default view. Legacy slugs from old bookmarks ("all"; "variations" has its own
    // literal route on the Variations page, which wins over this {Kind} template) land on RFIs.
    private string ActiveSlug => Kind?.ToLowerInvariant() == "general" ? "general" : "rfis";

    private enum StatusView { Open, Closed, All }

    private static readonly StatusView[] StatusViews = { StatusView.Open, StatusView.Closed, StatusView.All };

    // Status filter defaults to Open so the register opens on outstanding work.
    private StatusView statusView = StatusView.Open;

    // Records for the active kind tab, before the open/closed filter is applied.
    private IReadOnlyList<Request> KindRecords
    {
        get
        {
            var filter = Filters.FirstOrDefault(f => f.Slug == ActiveSlug);
            if (filter?.Kind is null) return RequestRegister.ForProject(ProjectId);
            return RequestRegister.ForProject(ProjectId, filter.Kind.Value);
        }
    }

    private IReadOnlyList<Request> FilteredRecords
    {
        get
        {
            IEnumerable<Request> records = statusView switch
            {
                StatusView.Open   => KindRecords.Where(r => r.Status is not RequestStatus.Closed),
                StatusView.Closed => KindRecords.Where(r => r.Status is RequestStatus.Closed),
                _                 => KindRecords
            };
            return InRegisterOrder(records.Where(MatchesSearch));
        }
    }

    // ---- Free-text search ------------------------------------------------------------------------
    // The register list already carries every text field the API holds for a request, so searching
    // the body text costs nothing beyond a string scan — no extra fetch, no new endpoint. Keywords
    // are ANDed on whitespace (the same behaviour as the project to-do list's search), so each extra
    // word narrows rather than widens the result.
    //
    // Two things are deliberately *not* searched, because neither arrives with the register: the
    // itemised queries (the list endpoint returns Items as null) and the email conversation (fetched
    // per record). Searching either would mean one request per row, so they stay a detail-page
    // concern until there's a server-side search to do it properly.

    private string search = "";

    private bool Searching => !string.IsNullOrWhiteSpace(search);

    private void OnSearchInput(ChangeEventArgs e) => search = e.Value?.ToString() ?? "";

    private void ClearSearch() => search = "";

    private bool MatchesSearch(Request record)
    {
        if (!Searching) return true;

        var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token =>
            SearchableText(record).Any(field => field is not null && field.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    // Everything on the record a person might half-remember: how it's referenced, what it says, who
    // it went to, what came back, and the sections of the official document.
    private static IEnumerable<string?> SearchableText(Request record)
    {
        yield return record.Reference;
        yield return record.DisplayNumber;
        yield return record.Title;
        yield return record.Description;
        yield return record.DrawingRef;
        yield return record.RelatedDrawingSpec;
        yield return record.ResponseText;
        yield return record.InternalNotes;
        yield return record.ClientNotes;
        yield return record.BasisOfQueries;
        yield return record.ResponseActionRequired;
        yield return record.ImpactIfLate;
    }

    // Matches sitting outside the current status view. The register opens on Active, and much of
    // what anyone searches for is historic — so say how many are hiding rather than showing zero.
    private int HiddenByStatusCount =>
        !Searching || statusView == StatusView.All
            ? 0
            : KindRecords.Count(MatchesSearch) - FilteredRecords.Count;

    private int MatchCount => FilteredRecords.Count;

    private string SearchPlaceholder => ActiveSlug == "general"
        ? "Search text in requests…"
        : "Search text in RFIs…";

    // Register reads in reference order, ascending (RFI-001 at the top). The number is
    // parsed rather than string-compared so unpadded or suffixed references (RFI-49,
    // RFI-049A, RFI-1000) still land in numeric order; free-text or blank references
    // sort after the numbered run within their prefix.
    private static IReadOnlyList<Request> InRegisterOrder(IEnumerable<Request> records) =>
        records
            .OrderBy(r => ReferenceKey(r.Reference).Prefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => ReferenceKey(r.Reference).Number)
            .ThenBy(r => r.Reference, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static (string Prefix, int Number) ReferenceKey(string? reference)
    {
        var raw = (reference ?? "").Trim();
        if (raw.Length == 0) return ("\uFFFF", int.MaxValue); // blanks last

        var dash = raw.IndexOf('-');
        if (dash > 0)
        {
            var digits = new string(raw[(dash + 1)..].TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var number))
                return (raw[..dash], number);
        }
        return (raw, int.MaxValue); // unnumbered free text after numbered refs
    }

    // Counts follow the search, so the chips double as "where did my match go" signposting: search
    // for a boiler RFI closed last month and Active reads 0 while Closed reads 1.
    private int StatusViewCount(StatusView view) => view switch
    {
        StatusView.Open   => KindRecords.Count(r => r.Status is not RequestStatus.Closed && MatchesSearch(r)),
        StatusView.Closed => KindRecords.Count(r => r.Status is RequestStatus.Closed && MatchesSearch(r)),
        _                 => KindRecords.Count(MatchesSearch)
    };

    private static string StatusViewLabel(StatusView view) => view switch
    {
        StatusView.Open   => "Active",
        StatusView.Closed => "Closed",
        _                 => "All"
    };

    private string StatusViewClass(StatusView view) =>
        view == statusView
            ? "px-3 py-1.5 rounded-md bg-accent text-accent-ink font-medium"
            : "px-3 py-1.5 rounded-md text-content-muted hover:text-content hover:bg-surface-raised";

    private string HrefFor(string slug) =>
        slug == "rfis" ? $"/projects/{ProjectId}/requests" : $"/projects/{ProjectId}/requests/{slug}";

    private string FilterClass(string slug)
    {
        if (slug == ActiveSlug) return "px-3 py-1.5 rounded-md bg-accent text-accent-ink font-medium";
        return "px-3 py-1.5 rounded-md text-content-muted hover:text-content hover:bg-surface-raised";
    }

    // ---- Excel export ----------------------------------------------------------------------
    // One sheet, following the active tab's kind/status/search filters. Picking "Include entire
    // register" from the export menu overrides them all: every request (both kinds, open and
    // closed) — the Kind and Status columns carry the distinctions the on-screen filters make.

    private bool HasExportableRows => AllRecords.Count > 0;

    private ExcelWorkbook? BuildExportWorkbook(bool includeEntireRegister)
    {
        var workbook = new ExcelWorkbook();

        var requestRecords = includeEntireRegister ? InRegisterOrder(AllRecords) : FilteredRecords;
        if (requestRecords.Count > 0)
        {
            var sheet = workbook.AddSheet("Requests & RFIs",
                new ExcelColumn("Ref"),
                new ExcelColumn("Kind"),
                new ExcelColumn("Subject"),
                new ExcelColumn("Drawing / detail"),
                new ExcelColumn("Issued", ExcelFormat.Date),
                new ExcelColumn("Response due", ExcelFormat.Date),
                new ExcelColumn("Days out", ExcelFormat.Integer),
                new ExcelColumn("Value", ExcelFormat.Currency),
                new ExcelColumn("Status"));

            foreach (var record in requestRecords)
            {
                sheet.AddRow(
                    record.Reference,
                    record.Kind.DisplayName(),
                    record.Title,
                    record.DrawingRef,
                    (record.IssuedAt ?? record.RaisedAt).LocalDateTime,
                    record.ResponseDue?.LocalDateTime,
                    record.DaysOutstanding,
                    record.Value,
                    RequestStatusLabel(record.Status));
            }
        }

        return workbook.Sheets.Count == 0 ? null : workbook;
    }

    // Matches RequestTable's Status cell text.
    private static string RequestStatusLabel(RequestStatus status) => status.DisplayName();

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        RequestRegister.OnChange += StateHasChanged;
        Activity.OnChanged += StateHasChanged;
        // Refresh on entry: cached requests render immediately, then update when the
        // background reload lands — so navigating back to this tab never shows stale data.
        RequestRegister.Refresh(ProjectId);
        Activity.Refresh(ProjectId);    // Activity badges land in the background — absent until then.
        isLoaded = true;
    }

    public void Dispose()
    {
        RequestRegister.OnChange -= StateHasChanged;
        Activity.OnChanged -= StateHasChanged;
    }
}
