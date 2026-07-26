using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>Role gates for the labour tracking surfaces (scope §6: worker registry and rates
/// are managed by the FD and PM; rates and £ are commercial-team-only reads).
///
/// Every set here names Role.Admin explicitly, per the authorisation convention the rest of the
/// app follows (Drawings, Variations, Xero, Commercial, Clients). Labour predated that convention
/// and omitted it, so a directory Administrator — who sees the Labour tab and the Workers page,
/// because DesktopNavigation.CanSee bypasses every navigation gate for admins — was met with a
/// 403 on every write. Master admins (JpmsAdministrators) were unaffected: role resolution hands
/// them the whole enum. A gate the navigation does not mirror is a gate the user only meets after
/// they have filled the form in.</summary>
internal static class LabourRoleSets
{
    /// <summary>May create/edit workers, rates, project assignments, and rotate site tokens.</summary>
    public static readonly RoleSet ManageWorkers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    /// <summary>
    /// May adjust, approve and reject timesheets, and enter a day on a worker's behalf
    /// (approval-flows.md row 18: PM). The Finance Director is included by decision 2026-07-26:
    /// the FD already owns the worker registry and the rates those hours are costed at, so being
    /// unable to correct a missed sign-out against them was an inconsistency, not a control.
    /// </summary>
    public static readonly RoleSet ApproveTimesheets =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    /// <summary>May manage the settlement reconciliation (covers, variances).</summary>
    public static readonly RoleSet ManageSettlement =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    /// <summary>May log their own time on the My Day page. Site operatives are the primary
    /// audience; foremen and site managers can log their own days too. Admin is NOT included:
    /// this is the "my own hours" surface, and an admin has no worker record to log against —
    /// the page renders nothing for them either way.</summary>
    public static readonly RoleSet LogOwnTime =
        RoleSet.Of(JpmsRoles.SiteOperative, JpmsRoles.Foreman, JpmsRoles.SiteManager);
}
