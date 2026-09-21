using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// The contract context a valuation statement prints above its figures: the original contract
/// sum, the net variations and the revised contract sum. A Draft reads it from the bill as it
/// stands; a locked claim keeps the one it was locked with, because that is what the client was
/// told — a variation approved between lock and Confirm never rewrites an issued statement.
/// </summary>
internal sealed record ClaimContractContext(decimal ContractSum, decimal NetVariations, decimal RevisedContractSum)
{
    public static ClaimContractContext FromBill(IReadOnlyList<ValuationLineItem> bill)
    {
        var contractSum = ValuationCalculations.ContractSum(bill);
        var netVariations = ValuationCalculations.NetVariations(bill);
        return new ClaimContractContext(contractSum, netVariations, ValuationCalculations.RevisedContractSum(contractSum, netVariations));
    }

    public static ClaimContractContext AsLocked(ValuationClaimEntity claim, IReadOnlyList<ValuationLineItem> bill)
    {
        var hasNeverBeenLocked = claim.LockedAt is null;
        if (hasNeverBeenLocked) return FromBill(bill);
        return new ClaimContractContext(claim.ContractSum, claim.NetVariations, claim.RevisedContractSum);
    }

    public void WriteOnto(ValuationClaimEntity claim)
    {
        claim.ContractSum = ContractSum;
        claim.NetVariations = NetVariations;
        claim.RevisedContractSum = RevisedContractSum;
    }
}
