using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>Restating a locked claim's lines corrects a settled statement: the claim lifecycle's own gate.</summary>
public sealed class RestateValuationClaimLinesAuthorisation
{
    private readonly ValuationReportAuthorisation claimLifecycle;
    public RestateValuationClaimLinesAuthorisation(ValuationReportAuthorisation claimLifecycle) { this.claimLifecycle = claimLifecycle; }

    public bool Allows(SignedInUser user, RestateValuationClaimLines command) => claimLifecycle.Allows(user, command);
}
