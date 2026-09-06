
namespace Jewel.JPMS.Pages;

public partial class XeroTransactions
{
    private enum View { Transactions, BySite }

    // Session checked and the user is signed in. This is NOT "the transactions are here" — the
    // header renders at once and the figures stay behind their own gate.
    private bool isRefreshing;
    private string? statusFilter;
    private string search = string.Empty;
    private string? expandedId;
    private View activeView = View.Transactions;

    private XeroTransactionsSnapshot? Snapshot => Xero.Snapshot();

    // Voided and deleted invoices aren't costs at all, so this page hides them entirely —
    // the same statuses the ledger sync keeps out of the allocation queue. The raw snapshot
    // still contains them on purpose: the sync must see a post-allocation void to clean up
    // stored lines and refresh their status, so don't filter these at the Xero client.
    private static readonly HashSet<string> HiddenStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "VOIDED", "DELETED"
    };

    private IReadOnlyList<XeroTransaction> VisibleTransactions =>
        Snapshot?.Transactions.Where(transaction => !HiddenStatuses.Contains(transaction.Status)).ToList()
        ?? (IReadOnlyList<XeroTransaction>)Array.Empty<XeroTransaction>();

    // Mirrors the accountant's payable invoice summary basis: paid, authorised and awaiting-approval
    // (SUBMITTED) records all count as committed cost. DRAFT never counts (voided/deleted are
    // hidden from the page altogether).
    private static bool CountsInBreakdown(XeroTransaction transaction) =>
        transaction.Status is "AUTHORISED" or "PAID" or "SUBMITTED";

    private static bool IsCreditNote(XeroTransaction transaction) =>
        transaction.Type == "ACCPAYCREDIT";

    private static decimal Sign(XeroTransaction transaction) =>
        IsCreditNote(transaction) ? -1m : 1m;

    private int BillCount =>
        VisibleTransactions.Count(transaction => !IsCreditNote(transaction));

    private int CreditNoteCount =>
        VisibleTransactions.Count(IsCreditNote);

    /// <summary>Net value of awaiting-approval records — the usual gap to the accountant's reports.</summary>
    private decimal AwaitingApprovalNet =>
        VisibleTransactions
            .Where(transaction => transaction.Status == "SUBMITTED")
            .Sum(transaction => Sign(transaction) * transaction.SubTotal);

    /// <summary>Net value of draft records the breakdown never counts.</summary>
    private decimal OtherExcludedNet =>
        VisibleTransactions
            .Where(transaction => transaction.Status is not ("AUTHORISED" or "PAID" or "SUBMITTED"))
            .Sum(transaction => Sign(transaction) * transaction.SubTotal);

    private string FromDateText => (Snapshot?.FromDate)?.ToString("MMM yyyy") ?? "Jan 2023";

    private string FetchedText =>
        Snapshot?.FetchedAtUtc is { } fetched ? fetched.ToLocalTime().ToString("HH:mm") : "—";

    private IReadOnlyList<string> Statuses =>
        VisibleTransactions.Select(transaction => transaction.Status).Distinct().OrderBy(status => status).ToList();

    private IReadOnlyList<XeroTransaction> Filtered =>
        VisibleTransactions
            .Where(transaction => statusFilter is null || transaction.Status == statusFilter)
            .Where(MatchesSearch)
            .OrderByDescending(transaction => transaction.Date ?? DateTime.MinValue)
            .ToList();

    private bool MatchesSearch(XeroTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        return Contains(transaction.ContactName) || Contains(transaction.Number) || Contains(transaction.Reference);

        bool Contains(string? value) =>
            value is not null && value.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<int> Years
    {
        get
        {
            var fromYear = Snapshot?.FromDate?.Year ?? 2023;
            return Enumerable.Range(fromYear, DateTime.Today.Year - fromYear + 1).ToList();
        }
    }

    private sealed record CostCodeRow(string Name, IReadOnlyDictionary<int, decimal> ByYear)
    {
        public decimal ForYear(int year) => ByYear.TryGetValue(year, out var value) ? value : 0m;
        public decimal Total => ByYear.Values.Sum();
    }

    private sealed record SiteRow(string Name, IReadOnlyList<CostCodeRow> CostCodes)
    {
        public decimal ForYear(int year) => CostCodes.Sum(costCode => costCode.ForYear(year));
        public decimal Total => CostCodes.Sum(costCode => costCode.Total);
    }

    private IReadOnlyList<SiteRow> Breakdown
    {
        get
        {
            if (Snapshot is null) return Array.Empty<SiteRow>();

            // Approved records only — mirrors Xero's own reports, which exclude drafts and
            // records awaiting approval. Credit notes subtract.
            var cells = VisibleTransactions
                .Where(CountsInBreakdown)
                .SelectMany(transaction => transaction.Lines.Select(line => new
                {
                    Site = string.IsNullOrWhiteSpace(line.Site) ? "(no site)" : line.Site!,
                    CostCode = string.IsNullOrWhiteSpace(line.CostCode) ? "(no cost code)" : line.CostCode!,
                    Year = transaction.Date?.Year ?? 0,
                    Net = Sign(transaction) * line.LineAmount
                }))
                .Where(cell => cell.Year > 0);

            return cells
                .GroupBy(cell => cell.Site)
                .Select(siteGroup => new SiteRow(
                    siteGroup.Key,
                    siteGroup
                        .GroupBy(cell => cell.CostCode)
                        .Select(codeGroup => new CostCodeRow(
                            codeGroup.Key,
                            codeGroup.GroupBy(cell => cell.Year)
                                     .ToDictionary(yearGroup => yearGroup.Key, yearGroup => yearGroup.Sum(cell => cell.Net))))
                        .OrderByDescending(row => row.Total)
                        .ToList()))
                .OrderByDescending(row => row.Total)
                .ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Xero.OnChange += StateHasChanged;

        // Revalidate cached data in the background on tab entry (stale-while-revalidate) —
        // the store's fetch-once guard handles the very first load.
        _ = Xero.RefreshAsync();
    }

    private async Task ForceRefreshAsync()
    {
        isRefreshing = true;
        try { await Xero.RefreshAsync(force: true); }
        finally { isRefreshing = false; }
    }

    private void ToggleExpanded(string transactionId) =>
        expandedId = expandedId == transactionId ? null : transactionId;

    private void ToggleStatus(string status) =>
        statusFilter = statusFilter == status ? null : status;

    private string TabClass(View view) =>
        (activeView == view
            ? "chip chip-active"
            : "chip");

    private string ChipClassFor(string status) =>
        (statusFilter == status
            ? "chip chip-active"
            : "chip");

    private static string AccountText(XeroTransactionLine line) =>
        line.AccountCode is null
            ? "—"
            : line.AccountName is null ? line.AccountCode : $"{line.AccountCode} — {line.AccountName}";

    private static string DistinctText(IEnumerable<string?> values)
    {
        var distinct = values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList();
        return distinct.Count == 0 ? "—" : string.Join(", ", distinct);
    }


    private static string MoneyOrDash(decimal value) =>
        value == 0m ? "—" : WholeMoney(value);

    // One workbook, one sheet per view — both exported regardless of which tab is
    // active, each reusing the page's own computed lists so the export matches the
    // screen exactly. "Ignore search & status" (offered while either narrows the table)
    // exports every visible transaction instead; the breakdown pivot is always full.
    private ExcelWorkbook? BuildExportWorkbook(bool ignoreFilters)
    {
        if (Snapshot is null) return null;

        var transactions = ignoreFilters
            ? VisibleTransactions.OrderByDescending(transaction => transaction.Date ?? DateTime.MinValue).ToList()
            : Filtered;
        var breakdown = Breakdown;
        if (transactions.Count == 0 && breakdown.Count == 0) return null;

        var workbook = new ExcelWorkbook();

        var transactionsSheet = workbook.AddSheet("Transactions",
            new ExcelColumn("Date", ExcelFormat.Date),
            new ExcelColumn("Supplier"),
            new ExcelColumn("Number"),
            new ExcelColumn("Site"),
            new ExcelColumn("Cost code"),
            new ExcelColumn("Status"),
            new ExcelColumn("Net", ExcelFormat.Currency),
            new ExcelColumn("Total", ExcelFormat.Currency));
        foreach (var transaction in transactions)
        {
            transactionsSheet.AddRow(
                transaction.Date,
                transaction.ContactName,
                transaction.Number,
                DistinctText(transaction.Lines.Select(l => l.Site)),
                DistinctText(transaction.Lines.Select(l => l.CostCode)),
                transaction.Status,
                transaction.SubTotal,
                transaction.Total);
        }

        var years = Years;
        var pivotColumns = new List<ExcelColumn> { new("Site / cost code") };
        pivotColumns.AddRange(years.Select(year => new ExcelColumn(year.ToString(), ExcelFormat.Currency)));
        pivotColumns.Add(new ExcelColumn("Total", ExcelFormat.Currency));

        var pivotSheet = workbook.AddSheet("Site × cost code", pivotColumns.ToArray());
        foreach (var site in breakdown)
        {
            var siteCells = new List<object?> { site.Name };
            siteCells.AddRange(years.Select(year => (object?)site.ForYear(year)));
            siteCells.Add(site.Total);
            pivotSheet.AddRow(siteCells.ToArray());

            foreach (var costCode in site.CostCodes)
            {
                var codeCells = new List<object?> { "  " + costCode.Name };
                codeCells.AddRange(years.Select(year => (object?)costCode.ForYear(year)));
                codeCells.Add(costCode.Total);
                pivotSheet.AddRow(codeCells.ToArray());
            }
        }
        var totalCells = new List<object?> { "All sites" };
        totalCells.AddRange(years.Select(year => (object?)breakdown.Sum(site => site.ForYear(year))));
        totalCells.Add(breakdown.Sum(site => site.Total));
        pivotSheet.AddRow(totalCells.ToArray());

        return workbook;
    }

    public void Dispose() => Xero.OnChange -= StateHasChanged;
}
