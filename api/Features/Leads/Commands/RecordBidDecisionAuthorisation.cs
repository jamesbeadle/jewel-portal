using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordBidDecisionAuthorisation
{
    private static readonly RoleSet RolesThatMayDecideBids =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, RecordBidDecision command) =>
        RolesThatMayDecideBids.Includes(user.Role);
}
