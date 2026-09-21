namespace Jewel.JPMS.Models;

/// <summary>
/// Who reads the company directory. The full directory, with its contact details, is internal
/// only: an external session (client, architect, subcontractor) reads its own record through
/// its portal, never the whole list. Compliance paperwork is read by the roles who place or
/// check the work — Foreman is on the directory but not on the compliance register.
/// </summary>
public static class DirectoryRoles
{
    public static readonly RoleSet AllowedToList = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.Foreman);

    public static readonly RoleSet AllowedToReadCompliance = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);
}
