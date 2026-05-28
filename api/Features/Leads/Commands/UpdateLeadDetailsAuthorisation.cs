using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class UpdateLeadDetailsAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateLeads =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, UpdateLeadDetails command) =>
        RolesThatMayUpdateLeads.Includes(user.Role);
}
