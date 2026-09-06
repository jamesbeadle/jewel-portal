namespace Jewel.JPMS.Api.Features.Sales;

/// <summary>
/// Who works the Sales section (2026-09-06). Reads are AllInternal — a lead is not a secret from
/// staff. Writing leads, logging touches and writing strategies is the sales team: the
/// directors, the PM and QS who qualify and price, and the Sales &amp; Marketing desk. Deciding a
/// lead's outcome (Won creates a client and a project; Lost closes it) and changing a strategy's
/// status is the directors'. Administrators pass every gate (SignedInUserResolver grants them
/// all roles). Each set is referenced by BOTH the HTTP gates and the connector actions, so the
/// two surfaces cannot drift.
/// </summary>
public static class SalesRoles
{
    public static readonly RoleSet Readers = JpmsRoleSets.AllInternal;

    public static readonly RoleSet SalesTeam = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SalesMarketing);

    public static readonly RoleSet Deciders = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector);
}
