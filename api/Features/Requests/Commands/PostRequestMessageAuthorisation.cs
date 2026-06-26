using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PostRequestMessageAuthorisation
{
    // Any participant on a project may contribute to a request conversation: internal
    // staff plus the external parties a request is run with (architect, subcontractor, client).
    private static readonly RoleSet RolesThatMayPostMessages =
        RoleSet.Of(
            JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
            JpmsRoles.Estimator, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead,
            JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.Architect,
            JpmsRoles.Client, JpmsRoles.Subcontractor, JpmsRoles.Foreman);

    public bool Allows(SignedInUser user, PostRequestMessage command) => RolesThatMayPostMessages.IncludesAny(user.Roles);
}
