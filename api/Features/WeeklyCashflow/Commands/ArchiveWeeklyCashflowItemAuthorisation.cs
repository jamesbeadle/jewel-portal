using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class ArchiveWeeklyCashflowItemAuthorisation
{
    public bool Allows(SignedInUser user, ArchiveWeeklyCashflowItem command) =>
        WeeklyCashflowGates.WeeklyCashflowRoles.IncludesAny(user.Roles);
}
