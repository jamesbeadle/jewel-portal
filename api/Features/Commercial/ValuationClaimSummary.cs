using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial;

// Recomputes a claim's summary/retention footer from source (the project's line items
// and this claim's per-line entries) and writes the frozen totals onto the claim entity.
// Recomputing from source is what lets every claim reconcile to the spreadsheet.
internal static class ValuationClaimSummary
{
    public static async Task ApplyTotalsAsync(JpmsContext context, ValuationClaimEntity claim, CancellationToken cancellationToken)
    {
        var lineModels = (await context.ValuationLineItems
                .Where(line => line.ProjectId == claim.ProjectId)
                .ToListAsync(cancellationToken))
            .Select(line => line.ToModel())
            .ToList();

        var claimLineModels = (await context.ClaimLines
                .Where(line => line.ValuationClaimId == claim.ValuationClaimId)
                .ToListAsync(cancellationToken))
            .Select(line => line.ToModel())
            .ToList();

        // Certified to date = total valuation-invoiced to date: every invoice that has been issued
        // to the client (Issued or Paid). Draft (Raised) invoices don't count until issued.
        var certifiedToDate = await context.ValuationInvoices
            .Where(invoice => invoice.ProjectId == claim.ProjectId
                              && (invoice.Status == (int)ValuationInvoiceStatus.Issued
                                  || invoice.Status == (int)ValuationInvoiceStatus.Paid))
            .SumAsync(invoice => (decimal?)invoice.Amount, cancellationToken) ?? 0m;

        var contractSum = ValuationCalculations.ContractSum(lineModels);
        var netVariations = ValuationCalculations.NetVariations(lineModels);
        var worksComplete = ValuationCalculations.TotalWorksComplete(claimLineModels);
        var retentionHeld = ValuationCalculations.RetentionHeld(worksComplete, claim.RetentionPercent);
        // Retention release is triggered as a separate event; the By France report shows £-.
        const decimal retentionReleased = 0m;

        claim.ContractSum = contractSum;
        claim.NetVariations = netVariations;
        claim.RevisedContractSum = ValuationCalculations.RevisedContractSum(contractSum, netVariations);
        claim.TotalWorksComplete = worksComplete;
        claim.RetentionHeld = retentionHeld;
        claim.RetentionReleased = retentionReleased;
        claim.CertifiedToDate = certifiedToDate;
        claim.PaymentDueExVat = ValuationCalculations.PaymentDueExVat(worksComplete, retentionHeld, retentionReleased, certifiedToDate);
    }
}
