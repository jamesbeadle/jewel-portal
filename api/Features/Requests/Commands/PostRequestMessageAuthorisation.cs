using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PostRequestMessageAuthorisation
{
    // Any participant on a project may contribute to a request conversation: internal staff plus
    // the external parties a request is run with (architect, subcontractor). Clients post through
    // their own client-portal command (2026-08-31), which scopes the write to their own projects
    // and forces Shared visibility — this ungated-by-record path is closed to them.
    private static readonly RoleSet RolesThatMayPostMessages =
        RoleSet.Of(
            JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
            JpmsRoles.Estimator, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead,
            JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.Architect,
            JpmsRoles.Subcontractor, JpmsRoles.Foreman);

    public bool Allows(SignedInUser user, PostRequestMessage command) => RolesThatMayPostMessages.IncludesAny(user.Roles);
}
