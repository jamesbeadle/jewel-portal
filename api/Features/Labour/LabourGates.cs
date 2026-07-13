using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>Role gates for the labour tracking surfaces (scope §6: worker registry and rates
/// are managed by the FD and PM; rates and £ are commercial-team-only reads).</summary>
internal static class LabourRoleSets
{
    /// <summary>May create/edit workers, rates, project assignments, and rotate site tokens.</summary>
    public static readonly RoleSet ManageWorkers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    /// <summary>May adjust, approve and reject timesheets (approval-flows.md row 18: PM).</summary>
    public static readonly RoleSet ApproveTimesheets =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    /// <summary>May manage the settlement reconciliation (covers, variances).</summary>
    public static readonly RoleSet ManageSettlement = JpmsRoleSets.CommercialTeam;

    /// <summary>May log their own time on the My Day page. Site operatives are the primary
    /// audience; foremen and site managers can log their own days too.</summary>
    public static readonly RoleSet LogOwnTime =
        RoleSet.Of(JpmsRoles.SiteOperative, JpmsRoles.Foreman, JpmsRoles.SiteManager);
}
