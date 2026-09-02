using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;
using static Jewel.JPMS.Features.Commercial.ValuationReportDisplay;

namespace Jewel.JPMS.Components;

public partial class ValuationReportTable
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";
    [Parameter] public ValuationClaim? SelectedClaim { get; set; }
    // The claim immediately before SelectedClaim, so each line can show its previously-claimed
    // cumulative % next to what's being claimed now. Null for Claim 1 or when no claim is selected.
    [Parameter] public ValuationClaim? PreviousClaim { get; set; }
    [Parameter] public EventCallback<ValuationLineItem> OnEditLine { get; set; }
    // Rendered between the bill sections and the summary (e.g. the valuation invoices accordion).
    [Parameter] public RenderFragment? ExtraSections { get; set; }
    // Total issued/paid valuation invoices — drives "Certified to date" for live (draft/no-claim) views.
    // Gross certification to date (issued/paid invoice amounts plus their embedded deposit
    // credits) and the deposit credits alone — both fed by the page's invoice list.
    [Parameter] public decimal CertifiedToDate { get; set; }
    [Parameter] public decimal DepositCreditedToDate { get; set; }

    private IReadOnlyList<ValuationLineItem> lines = Array.Empty<ValuationLineItem>();
    private IReadOnlyList<ClaimLine> entries = Array.Empty<ClaimLine>();
    // Entries from the previous claim, keyed by line — the baseline for each line's "Prev. %".
    private IReadOnlyList<ClaimLine> previousEntries = Array.Empty<ClaimLine>();

    // Sections start collapsed so the four totals read at a glance; state survives re-renders.
    private readonly HashSet<ValuationElementType> expanded = new();

    private bool IsExpanded(ValuationElementType type) => expanded.Contains(type);

    private void Toggle(ValuationElementType type)
    {
        if (!expanded.Remove(type)) expanded.Add(type);
    }

    // Editable only while a Draft claim is selected.
    private bool CanEditEntries => SelectedClaim is { Status: ValuationClaimStatus.Draft };
    // Lines can be edited whenever no claim has locked them (no Preapproved/Confirmed claim selected is fine,
    // but bill edits are always allowed since they only affect future claims; keep simple: allow unless viewing a locked claim).
    private bool CanEditLines => SelectedClaim is null or { Status: ValuationClaimStatus.Draft };

    // The summary's figures — shared with the Cashflow tab (ValuationSummaryFigures) so the two
    // tabs can't drift.
    private ValuationSummaryFigures figures = ValuationSummaryFigures.For(
        Array.Empty<ValuationLineItem>(), Array.Empty<ClaimLine>(), null, 0m, 0m);

    protected override void OnInitialized()
    {
        // The cost-centre master loads in the background on first read; re-render when it
        // lands so the Variations section's cost-centre cells fill in.
        CostCenters.OnChange += OnCostCentresChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        // The project's VOs, so each Variation line can resolve the order it mirrors
        // (by VariationRef) and offer the admin-only inline revise below. The page is
        // recreated per project by KeyedPageRouteView, so once per lifetime is fresh enough.
        variationOrders = await Variations.ListForProjectAsync(ProjectId);
    }

    private void OnCostCentresChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => CostCenters.OnChange -= OnCostCentresChanged;

    protected override void OnParametersSet()
    {
        lines = Store.LinesFor(ProjectId);
        entries = SelectedClaim is null ? Array.Empty<ClaimLine>() : Store.EntriesFor(SelectedClaim.ValuationClaimId);
        // Fetch-once per claim id (revalidated by the store's Refresh); loads in the background and
        // re-renders via the parent's OnChange subscription, same as the current claim's entries.
        previousEntries = PreviousClaim is null ? Array.Empty<ClaimLine>() : Store.EntriesFor(PreviousClaim.ValuationClaimId);
        // Half-typed bulk values belong to the claim they were typed against — switching
        // claims mid-edit discards them rather than silently writing to the wrong claim.
        if (bulkEditing && SelectedClaim?.ValuationClaimId != bulkClaimId) CancelBulkEdit();
        Recompute();
    }

    private void Recompute() =>
        figures = ValuationSummaryFigures.For(lines, entries, SelectedClaim, CertifiedToDate, DepositCreditedToDate);

    private record Section(ValuationElementType Type, string Title, List<ValuationLineItem> Lines)
    {
        public decimal Amount => Lines.Where(l => l.CountsTowardTotals).Sum(l => l.LineAmount);
    }

    // Always render all four sections so the shape of the bill is visible even before
    // any lines exist in a given block. Variation lines sit in their consolidated order
    // (VariationRollUps: V-ref, then each variation/cost-centre row, its lines together) so
    // Enter-to-advance walks the rows exactly as they render — a line added to an earlier
    // variation later on still renders with its variation instead of dropping to the bottom.
    private IEnumerable<Section> Sections
    {
        get
        {
            Section Make(string title, ValuationElementType type) =>
                new(type, title, OrderedLinesOf(type));

            return new[]
            {
                Make("Contract Works", ValuationElementType.ContractWorks),
                Make("Provisional Sums", ValuationElementType.PcSum),
                Make("Contingency Sums", ValuationElementType.Contingency),
                Make("Variations", ValuationElementType.Variation)
            };
        }
    }

    private List<ValuationLineItem> OrderedLinesOf(ValuationElementType type)
    {
        var ofType = lines.Where(l => l.ElementType == type);
        if (type == ValuationElementType.Variation)
            return VariationRollUps.Build(ofType).SelectMany(rollUp => rollUp.Lines).ToList();
        return ofType.OrderBy(l => l.DisplayOrder).ToList();
    }

    private decimal ClaimedTotalFor(Section section) =>
        section.Lines.Where(l => l.CountsTowardTotals).Sum(ClaimedFor);

    private decimal PercentTotalFor(Section section)
    {
        var amount = section.Amount;
        return amount == 0m ? 0m : Math.Round(ClaimedTotalFor(section) / amount * 100m, 2);
    }

    // ---- Area titles --------------------------------------------------------
    // The sub-headings within a bill section (shared rule: ValuationReportAreas) —
    // the estimate section recorded on the line when there is one, otherwise the
    // line's cost-centre name. Rendered as title rows whenever the area changes.
    private string AreaTitleFor(ValuationLineItem line) =>
        ValuationReportAreas.TitleFor(line.SectionName, line.CostCode, CostCentreNameFor);

    private string? CostCentreNameFor(string code) =>
        CostCenters.All().FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))?.Name;

    // How many columns the table currently renders — the area title row must span them all.
    // Base: Code, Description, Cost centre, Qty, Rate, Amount, % Complete, Claimed = 8;
    // a selected claim adds Prev. % and Period, edit rights add the actions column.
    private int ColumnCount => 8 + (SelectedClaim is not null ? 2 : 0) + (CanEditLines ? 1 : 0);

    // Non-variation lines show the Jewel master cost code (00001..00137, per the
    // seeded CostCenters master); fall back to the NRM2 section ref only if a line
    // has no cost code mapped. Variation lines prefer their VariationRef but now
    // also carry a cost code for purchase-invoice reconciliation.
    private static string CodeFor(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : line.VariationRef)
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    // Variation lines lead with their own line description (a multi-line VO reads as its
    // distinct items, not the VO title repeated); the VO title is only the fallback for
    // lines that never carried a description of their own.
    private static string TitleFor(ValuationLineItem line)
    {
        if (line.ElementType == ValuationElementType.Variation)
            return string.IsNullOrWhiteSpace(line.Description) ? line.VariationTitle : line.Description;
        if (!string.IsNullOrWhiteSpace(line.Description)) return line.Description;
        return line.SectionName;
    }

    private decimal PercentFor(ValuationLineItem line) =>
        entries.FirstOrDefault(e => e.ValuationLineItemId == line.ValuationLineItemId)?.PercentComplete ?? 0m;

    private decimal ClaimedFor(ValuationLineItem line) =>
        ValuationCalculations.CumulativeClaimed(PercentFor(line), line.LineAmount);
}
