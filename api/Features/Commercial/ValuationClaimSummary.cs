using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;

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

        // Certified to date = GROSS certification: every issued/paid invoice's cash amount
        // plus the deposit credit embedded in it (the certificate before the deposit came
        // off). Draft (Raised) invoices don't count until issued.
        var issuedInvoices = await context.ValuationInvoices
            .Where(invoice => invoice.ProjectId == claim.ProjectId
                              && (invoice.Status == (int)ValuationInvoiceStatus.Issued
                                  || invoice.Status == (int)ValuationInvoiceStatus.Paid))
            .Select(invoice => new { invoice.Amount, invoice.DepositCredited })
            .ToListAsync(cancellationToken);
        var certifiedToDate = issuedInvoices.Sum(invoice => invoice.Amount + invoice.DepositCredited);
        var depositCreditedToDate = issuedInvoices.Sum(invoice => invoice.DepositCredited);

        var contractSum = ValuationCalculations.ContractSum(lineModels);
        var netVariations = ValuationCalculations.NetVariations(lineModels);
        var worksComplete = ValuationCalculations.TotalWorksComplete(claimLineModels);
        var retentionHeld = ValuationCalculations.RetentionHeld(worksComplete, claim.RetentionPercent);
        // Retention release adds back only when the claim carries a release % — stamped
        // solely once the claim date has reached practical completion (pre-completion
        // claims carry 0% and reconcile to the By France workbook's £-). Post-completion
        // the payment due is works less NET retention, matching the architect's interim
        // certificate convention (e.g. PLG's PC certificate: gross less 2.5%).
        var retentionReleased = ValuationCalculations.RetentionReleased(worksComplete, claim.RetentionReleasePercent);

        // Cash-up-front deposit: released back pro rata against the contract-side works
        // (contract works + PC sums + contingency — variations excluded), capped at the
        // deposit received (deposit % of the contract sum). What the claim still deducts
        // is the release earned to date LESS the opening balance settled before tracking
        // LESS credits already embedded in issued/paid invoices — zero once the period's
        // invoice is out, so a freshly rolled claim starts clean.
        var nonVariationWorks = ValuationCalculations.NonVariationWorksComplete(claimLineModels, lineModels);
        var depositReceived = ValuationCalculations.DepositReceived(contractSum, claim.DepositPercent);
        var depositReleased = ValuationCalculations.DepositDeduction(
            ValuationCalculations.DepositReleased(nonVariationWorks, claim.DepositPercent, depositReceived),
            claim.DepositReleasedOpening, depositCreditedToDate);

        claim.ContractSum = contractSum;
        claim.NetVariations = netVariations;
        claim.RevisedContractSum = ValuationCalculations.RevisedContractSum(contractSum, netVariations);
        claim.TotalWorksComplete = worksComplete;
        claim.RetentionHeld = retentionHeld;
        claim.RetentionReleased = retentionReleased;
        claim.DepositReleased = depositReleased;
        claim.CertifiedToDate = certifiedToDate;
        claim.PaymentDueExVat = ValuationCalculations.PaymentDueExVat(worksComplete, retentionHeld, retentionReleased, depositReleased, certifiedToDate);
    }
}
