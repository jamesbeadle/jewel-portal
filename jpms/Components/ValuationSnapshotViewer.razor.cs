using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Services.Excel;

namespace Jewel.JPMS.Components;

public partial class ValuationSnapshotViewer
{
    [Parameter, EditorRequired] public string SnapshotId { get; set; } = "";

    /// <summary>Wired by pages that host the email-draft modal: opens the draft flow for this
    /// snapshot. Left unwired, the button simply doesn't render.</summary>
    [Parameter] public EventCallback<string> OnEmailRequested { get; set; }

    private static readonly System.Globalization.CultureInfo Gb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");

    private ValuationReportSnapshotDetail? detail;
    private bool loadFailed;
    private string? loadedSnapshotId;

    private string ExportFileName(ValuationReportSnapshot snapshot) =>
        ValuationReportFileNames.Stem(Projects.Find(snapshot.ProjectId)?.Reference ?? "", snapshot.Label);

    // Null until the mailbox has ANSWERED — an empty list is a real "none", never a placeholder
    // (the loading convention: nullable backing field, gate on the null).
    private IReadOnlyList<MailboxMessage>? emails;
    private string? emailsError;

    // ---- Pending variations for the export's Pending tab --------------------
    // A snapshot freezes the report, not the register, so the workbook's Pending tab reads the
    // LIVE register at export time (the tab says so). Nullable until answered (the loading
    // convention); on failure the export still runs and the tab reports the register unread.
    private IReadOnlyList<VariationOrder>? variationOrders;
    private bool variationOrdersFailed;
    private string? variationOrdersProjectId;

    private bool PendingVariationsUnanswered => variationOrders is null && !variationOrdersFailed;

    private async Task LoadVariationOrdersAsync(string projectId)
    {
        if (projectId == variationOrdersProjectId) return;
        variationOrdersProjectId = projectId;
        variationOrders = null;
        variationOrdersFailed = false;
        try { variationOrders = await Variations.ListForProjectAsync(projectId); }
        catch { variationOrdersFailed = true; }
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (SnapshotId == loadedSnapshotId) return;
        loadedSnapshotId = SnapshotId;
        detail = null;
        loadFailed = false;
        emails = null;
        emailsError = null;
        try { detail = await Store.GetSnapshotAsync(SnapshotId); }
        catch { loadFailed = true; }
        if (detail is not null) _ = LoadVariationOrdersAsync(detail.Snapshot.ProjectId);
        await LoadEmailsAsync();
    }

    // The emails triaged to this snapshot's tag, read live — a failure opens the gate with its
    // own message rather than blocking the report above (the frozen lines are the main event).
    private async Task LoadEmailsAsync()
    {
        try
        {
            emails = await Queries.AskAsync(
                new ListRecordEmails(RecordType.ValuationReportSnapshot, SnapshotId), CancellationToken.None);
        }
        catch
        {
            emailsError = "Couldn't load this snapshot's correspondence. Please try again.";
        }
    }

    private record Section(ValuationElementType Type, string Title, List<ValuationReportSnapshotLine> Lines)
    {
        public decimal Amount => Lines.Where(l => l.CountsTowardTotals).Sum(l => l.LineAmount);
        public decimal Claimed => Lines.Where(l => l.CountsTowardTotals).Sum(l => l.CumulativeClaimed);
    }

    private IEnumerable<Section> Sections
    {
        get
        {
            if (detail is null) yield break;
            Section Make(string title, ValuationElementType type) =>
                new(type, title, detail.Lines.Where(l => l.ElementType == type)
                                             .OrderBy(l => l.DisplayOrder)
                                             .ToList());
            yield return Make("Contract Works", ValuationElementType.ContractWorks);
            yield return Make("Provisional Sums", ValuationElementType.PcSum);
            yield return Make("Contingency Sums", ValuationElementType.Contingency);
            yield return Make("Variations", ValuationElementType.Variation);
        }
    }

    // ---- Area titles --------------------------------------------------------
    // Same shared rule as the live report table: the estimate section frozen on the line,
    // else the cost-centre name from the master. The master loads in the background —
    // re-render when it lands so fallback titles fill in (a snapshot is read-only, so a
    // late name never changes any figure).
    private string AreaTitleFor(ValuationReportSnapshotLine line) =>
        ValuationReportAreas.TitleFor(line.SectionName, line.CostCode, CostCentreNameFor);

    private string? CostCentreNameFor(string code) =>
        CostCenters.All().FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))?.Name;

    protected override void OnInitialized()
    {
        CostCenters.OnChange += OnCostCentresChanged;
        // The export file name carries the project reference — re-render when the list lands.
        Projects.OnChanged += OnCostCentresChanged;
        // Revalidate the master (stale-while-revalidate; fetch-once underneath) — the host
        // page usually warmed it already, but the snapshots register opens this viewer too.
        _ = CostCenters.ListAllAsync();
    }

    private void OnCostCentresChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        CostCenters.OnChange -= OnCostCentresChanged;
        Projects.OnChanged -= OnCostCentresChanged;
    }

    private static string CodeFor(ValuationReportSnapshotLine line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : VariationRefs.Padded(line.VariationRef))
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    // Variation lines lead with their own line description; VO title is the fallback —
    // mirrors ValuationReportTable so the snapshot reads like the live report.
    private static string TitleFor(ValuationReportSnapshotLine line)
    {
        if (line.ElementType == ValuationElementType.Variation)
            return string.IsNullOrWhiteSpace(line.Description) ? line.VariationTitle : line.Description;
        if (!string.IsNullOrWhiteSpace(line.Description)) return line.Description;
        return line.SectionName;
    }

    private static string Num(decimal v) => v.ToString("0.##", Gb);
    private static string Pct(decimal v) => v.ToString("0.##", Gb) + "%";

    // Every frozen line plus the summary footer, so the workbook and the screen (and the PDF,
    // rendered from the same lines) always agree. The workbook is the shared shape (Summary with
    // variations as one row per order, one tab per variation order carrying its lines, and the
    // Pending variations tab read from the live register) and does its own consolidating; each
    // line's "Previous" is its cumulative claimed less the period increment frozen at capture.
    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        if (detail is null) return null;
        var snapshot = detail.Snapshot;

        var exportLines = new List<ValuationExportLine>();
        foreach (var section in Sections)
        {
            foreach (var line in section.Lines)
            {
                exportLines.Add(ExportLineFor(section, line));
            }
        }

        var periodTotal = detail.Lines
            .Where(line => line.CountsTowardTotals)
            .Sum(line => line.PeriodIncrement);

        var summary = new List<ValuationExportSummaryRow>
        {
            new("Original contract sum", snapshot.ContractSum),
            new("Net variations", snapshot.NetVariations),
            new("Revised contract sum", snapshot.RevisedContractSum, Strong: true),
            new("Total works complete", snapshot.TotalWorksComplete),
            new("Works claimed this period", periodTotal),
            new($"Retention held ({Pct(snapshot.RetentionPercent)})", snapshot.RetentionHeld),
            new($"Retention released ({Pct(snapshot.RetentionReleasePercent)})", snapshot.RetentionReleased),
            new("Certified to date", snapshot.CertifiedToDate),
        };
        if (snapshot.DepositPercent > 0m || snapshot.DepositReleased != 0m)
        {
            summary.Add(new("Payment due before deposit (ex VAT)", snapshot.PaymentDueExVat + snapshot.DepositReleased));
            summary.Add(new($"Less deposit released ({Pct(snapshot.DepositPercent)})", snapshot.DepositReleased));
        }
        summary.Add(new("Payment due (ex VAT)", snapshot.PaymentDueExVat, Strong: true));

        return ValuationReportExportWorkbook.Build(
            new ValuationExportMeta(
                snapshot.Label,
                $"Snapshot taken {snapshot.TakenAt.ToString("dd MMM yyyy HH:mm", Gb)} · immutable record from the JPMS register",
                IsDraft: false),
            exportLines,
            summary,
            variationOrders is null ? null : ValuationExportPendingVariations.From(variationOrders));
    }

    private ValuationExportLine ExportLineFor(Section section, ValuationReportSnapshotLine line) =>
        new(section.Title,
            line.ElementType,
            ValuationReportAreas.GroupsByArea(section.Type) ? AreaTitleFor(line) : "",
            CodeFor(line),
            TitleFor(line),
            LineTypeLabel(line.LineType),
            line.CountsTowardTotals,
            line.Unit,
            line.Quantity,
            line.Rate,
            line.LineAmount,
            line.PercentComplete,
            line.CumulativeClaimed - line.PeriodIncrement,
            line.PeriodIncrement,
            line.CumulativeClaimed,
            line.Comments,
            line.VariationRef,
            line.VariationTitle,
            line.CostCode,
            line.DisplayOrder,
            line.ClientReference);

    // Same wording as the PDF renderer, so the workbook and the statement agree.
    private static string LineTypeLabel(ValuationLineType type) => type switch
    {
        ValuationLineType.Priced => "Priced",
        ValuationLineType.ProvisionalSum => "Provisional sum",
        ValuationLineType.Omit => "Omit",
        ValuationLineType.Declined => "Declined",
        ValuationLineType.Tbc => "TBC",
        _ => type.ToString()
    };
}
