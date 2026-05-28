using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class ReviseProposalAuthorisation
{
    private static readonly RoleSet RolesThatMayReviseProposals =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, ReviseProposal command) =>
        RolesThatMayReviseProposals.IncludesAny(user.Roles);
}
