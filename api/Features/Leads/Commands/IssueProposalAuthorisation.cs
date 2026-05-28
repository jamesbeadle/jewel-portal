using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class IssueProposalAuthorisation
{
    private static readonly RoleSet RolesThatMayIssueProposals =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, IssueProposal command) =>
        RolesThatMayIssueProposals.IncludesAny(user.Roles);
}
