using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// The undo for an unintended "We're claiming this": Preapproved -> Draft. Clears the
// preapproval stamp and zeroes the frozen totals — a Draft's figures compute live from
// its entries, exactly as before preapproval, so nothing else needs recomputing.
// Confirmed claims are final (their amounts advanced CertifiedToDate) and cannot reopen.
public sealed class ReopenValuationClaimHandler : ICommandHandler<ReopenValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    public ReopenValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationClaim> HandleAsync(ReopenValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        if (entity.Status == (int)ValuationClaimStatus.Confirmed)
            throw new InvalidOperationException("A confirmed claim is final — its amounts have advanced certified to date and it cannot be reopened.");
        if (entity.Status != (int)ValuationClaimStatus.Preapproved)
            throw new InvalidOperationException("Only a Preapproved claim can be reopened to Draft.");

        entity.Status = (int)ValuationClaimStatus.Draft;
        entity.PreapprovedAt = null;
        // Frozen totals go back to zero, matching a freshly started claim: Draft views
        // compute the summary live from the line entries.
        entity.ContractSum = 0m;
        entity.NetVariations = 0m;
        entity.RevisedContractSum = 0m;
        entity.TotalWorksComplete = 0m;
        entity.RetentionHeld = 0m;
        entity.RetentionReleased = 0m;
        entity.CertifiedToDate = 0m;
        entity.PaymentDueExVat = 0m;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
