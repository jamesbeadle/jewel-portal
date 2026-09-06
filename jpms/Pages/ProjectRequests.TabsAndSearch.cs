using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequests
{
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

    private void OnSearchInput(string value) => search = value;

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
            ? "px-3 py-1.5 rounded bg-accent text-accent-ink font-medium"
            : "px-3 py-1.5 rounded text-content-muted hover:text-content hover:bg-surface-raised";

    private string HrefFor(string slug) =>
        slug == "rfis" ? $"/projects/{ProjectId}/requests" : $"/projects/{ProjectId}/requests/{slug}";

    private string FilterClass(string slug)
    {
        if (slug == ActiveSlug) return "chip chip-active";
        return "chip";
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
    }

    public void Dispose()
    {
        RequestRegister.OnChange -= StateHasChanged;
        Activity.OnChanged -= StateHasChanged;
    }
}
