using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Writes a claim's frozen footer from source — the project's bill and this claim's own rows —
/// which is what lets every claim reconcile to the spreadsheet. Two moments call it: the lock
/// (<see cref="FreezeTotalsAsync"/>), which reads everything from the bill as it stands, and
/// every later re-freeze (<see cref="RefreshTotalsAsync"/>) when the certified total moves,
/// which keeps the contract context the claim was locked with (<see cref="ClaimContractContext"/>).
/// </summary>
internal static class ValuationClaimSummary
{
    public static async Task FreezeTotalsAsync(JpmsContext context, ValuationClaimEntity claim, CancellationToken cancellationToken)
    {
        var bill = await BillAsync(context, claim, cancellationToken);
        await ApplyAsync(context, claim, bill, ClaimContractContext.FromBill(bill), cancellationToken);
    }

    public static async Task RefreshTotalsAsync(JpmsContext context, ValuationClaimEntity claim, CancellationToken cancellationToken)
    {
        var bill = await BillAsync(context, claim, cancellationToken);
        await ApplyAsync(context, claim, bill, ClaimContractContext.AsLocked(claim, bill), cancellationToken);
    }

    private static async Task ApplyAsync(
        JpmsContext context, ValuationClaimEntity claim, IReadOnlyList<ValuationLineItem> bill,
        ClaimContractContext contractContext, CancellationToken cancellationToken)
    {
        var claimLines = await ClaimLinesAsync(context, claim, cancellationToken);
        var certification = await CertifiedBeforeClaim.ForAsync(context, claim.ProjectId, claim.ClaimNumber, cancellationToken);
        var worksComplete = ValuationCalculations.TotalWorksComplete(claimLines);
        var retentionHeld = ValuationCalculations.RetentionHeld(worksComplete, claim.RetentionPercent);
        var retentionReleased = ValuationCalculations.RetentionReleased(worksComplete, claim.RetentionReleasePercent);
        var depositReleased = DepositReleased(claim, claimLines, bill, contractContext.ContractSum, certification.DepositCreditedToDate);

        contractContext.WriteOnto(claim);
        claim.TotalWorksComplete = worksComplete;
        claim.RetentionHeld = retentionHeld;
        claim.RetentionReleased = retentionReleased;
        claim.DepositReleased = depositReleased;
        claim.CertifiedToDate = certification.CertifiedToDate;
        claim.PaymentDueExVat = ValuationCalculations.PaymentDueExVat(
            worksComplete, retentionHeld, retentionReleased, depositReleased, certification.CertifiedToDate);
    }

    /// <summary>
    /// The deposit still deducted: the release earned to date against the contract-side works,
    /// less the opening balance settled before tracking, less the credits already embedded in
    /// issued invoices — zero once the period's invoice is out.
    /// </summary>
    private static decimal DepositReleased(
        ValuationClaimEntity claim, IReadOnlyList<ClaimLine> claimLines, IReadOnlyList<ValuationLineItem> bill,
        decimal contractSum, decimal depositCreditedToDate)
    {
        var nonVariationWorks = ValuationCalculations.NonVariationWorksComplete(claimLines, bill);
        var depositReceived = ValuationCalculations.DepositReceived(contractSum, claim.DepositPercent);
        var releasedToDate = ValuationCalculations.DepositReleased(nonVariationWorks, claim.DepositPercent, depositReceived);
        return ValuationCalculations.DepositDeduction(releasedToDate, claim.DepositReleasedOpening, depositCreditedToDate);
    }

    private static async Task<IReadOnlyList<ValuationLineItem>> BillAsync(
        JpmsContext context, ValuationClaimEntity claim, CancellationToken cancellationToken)
    {
        var lines = await context.ValuationLineItems
            .Where(line => line.ProjectId == claim.ProjectId)
            .ToListAsync(cancellationToken);
        return lines.Select(line => line.ToModel()).ToList();
    }

    private static async Task<IReadOnlyList<ClaimLine>> ClaimLinesAsync(
        JpmsContext context, ValuationClaimEntity claim, CancellationToken cancellationToken)
    {
        var rows = await context.ClaimLines
            .Where(row => row.ValuationClaimId == claim.ValuationClaimId)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}
