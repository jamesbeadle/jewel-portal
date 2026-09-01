using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class CreateWeeklyCashflowItemAuthorisation
{
    public bool Allows(SignedInUser user, CreateWeeklyCashflowItem command) =>
        WeeklyCashflowGates.WeeklyCashflowRoles.IncludesAny(user.Roles);
}
