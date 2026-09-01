using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class SetWeeklyCashflowExclusionAuthorisation
{
    public bool Allows(SignedInUser user, SetWeeklyCashflowExclusion command) =>
        WeeklyCashflowGates.WeeklyCashflowRoles.IncludesAny(user.Roles);
}
