using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class CaptureLeadAuthorisation
{
    private static readonly RoleSet RolesThatMayCaptureLeads =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, CaptureLead command) =>
        RolesThatMayCaptureLeads.Includes(user.Role);
}
