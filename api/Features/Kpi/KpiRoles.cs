namespace Jewel.JPMS.Api.Features.Kpi;

/// <summary>
/// The KPI register is administrators-only, end to end (James, 2026-09-03: "only admins can see
/// this"). ONE role, deliberately narrower than <see cref="AdminGate"/> (which also admits the
/// Finance Director) and never widened by an effective-roles expansion: a non-administrator's
/// effective role list can never contain <see cref="Role.Admin"/>, so this set is a true
/// administrator test wherever it is used — endpoints, connector tools and actions alike.
/// </summary>
public static class KpiRoles
{
    public static readonly RoleSet Administrators = RoleSet.Of(Role.Admin);

    public static bool IsAdministrator(SignedInUser user) => user.Roles.Contains(Role.Admin);
}
