using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class UpdateWeeklyCashflowItemAuthorisation
{
    public bool Allows(SignedInUser user, UpdateWeeklyCashflowItem command) =>
        WeeklyCashflowGates.WeeklyCashflowRoles.IncludesAny(user.Roles);
}
