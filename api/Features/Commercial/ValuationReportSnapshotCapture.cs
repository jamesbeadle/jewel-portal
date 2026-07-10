using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Freezes an immutable, line-level copy of a project's valuation report as it stands right now:
/// every priced line with the % complete / cumulative claimed from the project's latest claim
/// (missing entries count as 0%), plus the summary footer with "Certified to date" stamped from
/// Issued+Paid valuation invoices at this moment. Values are copied, never referenced — later
/// edits or deletions of live lines must not disturb what was submitted to the client.
///
/// Adds the snapshot and its lines to the change tracker but does NOT save; callers (invoice
/// submission/issue, on-demand capture) save in their own transaction. When the snapshot backs an
/// invoice, any earlier snapshots for the same invoice are flagged superseded in the same save.
/// </summary>
internal static class ValuationReportSnapshotCapture
{
    public static async Task<ValuationReportSnapshotEntity> CaptureAsync(
        JpmsContext context,
        string projectId,
        string label,
        string? valuationInvoiceId,
        CancellationToken cancellationToken)
    {
        var lines = await context.ValuationLineItems
            .Where(line => line.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        // The figures come from the latest claim (highest number), whatever its status —
        // that is what the report tab shows and what a submission is asking to be paid for.
        var claim = await context.ValuationClaims
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.ClaimNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var entriesByLineItem = claim is null
            ? new Dictionary<string, ClaimLineEntity>()
            : await context.ClaimLines
                .Where(entry => entry.ValuationClaimId == claim.ValuationClaimId)
                .ToDictionaryAsync(entry => entry.ValuationLineItemId, cancellationToken);

        var certifiedToDate = await context.ValuationInvoices
            .Where(invoice => invoice.ProjectId == projectId
                              && (invoice.Status == (int)ValuationInvoiceStatus.Issued
                                  || invoice.Status == (int)ValuationInvoiceStatus.Paid))
            .SumAsync(invoice => (decimal?)invoice.Amount, cancellationToken) ?? 0m;

        var lineModels = lines.Select(line => line.ToModel()).ToList();
        var contractSum = ValuationCalculations.ContractSum(lineModels);
        var netVariations = ValuationCalculations.NetVariations(lineModels);

        var snapshot = new ValuationReportSnapshotEntity
        {
            ValuationReportSnapshotId = CommercialIdentifierFactory.NextValuationReportSnapshotId(),
            ProjectId = projectId,
            ValuationInvoiceId = valuationInvoiceId,
            ValuationClaimId = claim?.ValuationClaimId,
            Label = label,
            TakenAt = DateTimeOffset.UtcNow,
            IsSuperseded = false,
            ContractSum = contractSum,
            NetVariations = netVariations,
            RevisedContractSum = ValuationCalculations.RevisedContractSum(contractSum, netVariations),
            RetentionPercent = claim?.RetentionPercent ?? 0m,
            RetentionReleasePercent = claim?.RetentionReleasePercent ?? 0m,
            // Retention release is triggered as a separate event; mirrors ValuationClaimSummary.
            RetentionReleased = 0m,
            CertifiedToDate = certifiedToDate
        };

        var totalWorksComplete = 0m;
        var displayOrder = 0;
        foreach (var line in lines.OrderBy(line => line.ElementType).ThenBy(line => line.DisplayOrder))
        {
            entriesByLineItem.TryGetValue(line.ValuationLineItemId, out var entry);
            var snapshotLine = new ValuationReportSnapshotLineEntity
            {
                ValuationReportSnapshotLineId = CommercialIdentifierFactory.NextValuationReportSnapshotLineId(),
                ValuationReportSnapshotId = snapshot.ValuationReportSnapshotId,
                SourceValuationLineItemId = line.ValuationLineItemId,
                ElementType = line.ElementType,
                SectionCode = line.SectionCode,
                SectionName = line.SectionName,
                VariationRef = line.VariationRef,
                VariationTitle = line.VariationTitle,
                LineType = line.LineType,
                CostCode = line.CostCode,
                Description = line.Description,
                Unit = line.Unit,
                Quantity = line.Quantity,
                Rate = line.Rate,
                LineAmount = line.LineAmount,
                PercentComplete = entry?.PercentComplete ?? 0m,
                CumulativeClaimed = entry?.CumulativeClaimed ?? 0m,
                PeriodIncrement = entry?.PeriodIncrement ?? 0m,
                Comments = line.Comments,
                DisplayOrder = displayOrder++
            };
            // Declined/TBC lines are recorded but never priced into totals — keep the
            // footer reconciling with the viewer's per-section sums.
            var countsTowardTotals = snapshotLine.LineType is not ((int)ValuationLineType.Declined or (int)ValuationLineType.Tbc);
            if (countsTowardTotals) totalWorksComplete += snapshotLine.CumulativeClaimed;
            context.ValuationReportSnapshotLines.Add(snapshotLine);
        }

        snapshot.TotalWorksComplete = totalWorksComplete;
        snapshot.RetentionHeld = ValuationCalculations.RetentionHeld(totalWorksComplete, snapshot.RetentionPercent);
        snapshot.PaymentDueExVat = ValuationCalculations.PaymentDueExVat(
            totalWorksComplete, snapshot.RetentionHeld, snapshot.RetentionReleased, certifiedToDate);

        context.ValuationReportSnapshots.Add(snapshot);

        if (valuationInvoiceId is not null)
        {
            var earlier = await context.ValuationReportSnapshots
                .Where(s => s.ValuationInvoiceId == valuationInvoiceId && !s.IsSuperseded)
                .ToListAsync(cancellationToken);
            foreach (var previous in earlier.Where(s => s.ValuationReportSnapshotId != snapshot.ValuationReportSnapshotId))
                previous.IsSuperseded = true;
        }

        return snapshot;
    }
}
