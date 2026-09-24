using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PostRequestMessageAuthorisation
{
    // Any participant on a project may contribute to a request conversation: internal staff plus
    // the external parties a request is run with (client, architect, subcontractor). The endpoint
    // confines each to its own records (RequestScope) and the handler makes whatever an external
    // party posts Shared (SignedInCaller).
    private static readonly RoleSet RolesThatMayPostMessages =
        RoleSet.Of(
            JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
            JpmsRoles.Estimator, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead,
            JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.Architect,
            JpmsRoles.Client, JpmsRoles.Subcontractor, JpmsRoles.Foreman);

    public bool Allows(SignedInUser user, PostRequestMessage command) => RolesThatMayPostMessages.IncludesAny(user.Roles);
}
