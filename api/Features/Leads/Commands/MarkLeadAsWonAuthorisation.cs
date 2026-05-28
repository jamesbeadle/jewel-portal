using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class MarkLeadAsWonAuthorisation
{
    private static readonly RoleSet RolesThatMayMarkLeadsAsWon =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, MarkLeadAsWon command) =>
        RolesThatMayMarkLeadsAsWon.Includes(user.Role);
}
