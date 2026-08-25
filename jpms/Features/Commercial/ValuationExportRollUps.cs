using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>
/// The consolidated variation row as the Excel exports carry it — one line per variation order
/// per cost centre, matching the PDF the client receives. The caller supplies the claim figures
/// (live entries or frozen snapshot values); this only shapes the row.
/// </summary>
public static class ValuationExportRollUps
{
    private const string ConsolidatedLabel = "Consolidated";

    public static ValuationExportLine Line<TLine>(
        string sectionTitle,
        VariationRollUp<TLine> rollUp,
        string costCentreName,
        decimal percentComplete,
        decimal previousClaimed,
        decimal thisPeriod,
        decimal cumulativeClaimed) where TLine : IVariationBillLine =>
        new(sectionTitle,
            Area: "",
            Code: rollUp.VariationRef,
            Title: $"{rollUp.VariationTitle} ({rollUp.Lines.Count} lines)",
            LineTypeLabel: ConsolidatedLabel,
            CountsTowardTotals: rollUp.CountsTowardTotals,
            Unit: "",
            Quantity: null,
            Rate: null,
            LineAmount: rollUp.Amount,
            PercentComplete: percentComplete,
            PreviousClaimed: previousClaimed,
            ThisPeriod: thisPeriod,
            CumulativeClaimed: cumulativeClaimed,
            Comments: costCentreName);
}
